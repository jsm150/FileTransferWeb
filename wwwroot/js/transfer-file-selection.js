(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    var tusClientAvailable = typeof window.tus !== "undefined" && typeof window.tus.Upload === "function";

    var targetPathInput = document.querySelector("[data-role='target-path']");
    var openPathExplorerButton = document.querySelector("[data-role='open-path-explorer']");
    var pickFilesButton = document.querySelector("[data-role='pick-files']");
    var fileInput = document.querySelector("[data-role='file-input']");
    var fileList = document.querySelector("[data-role='selected-file-list']");
    var fileEmpty = document.querySelector("[data-role='file-empty']");
    var resetButton = document.querySelector("[data-role='reset-upload']");
    var startUploadButton = document.querySelector("[data-role='start-upload']");
    var selectedCount = document.querySelector("[data-role='selected-count']");
    var selectedSize = document.querySelector("[data-role='selected-size']");
    var completedCount = document.querySelector("[data-role='completed-count']");
    var filePickerHint = document.querySelector("[data-role='file-picker-hint']");
    var listTotal = document.querySelector("[data-role='list-total']");
    var listUploading = document.querySelector("[data-role='list-uploading']");
    var listFailed = document.querySelector("[data-role='list-failed']");
    var overallPercent = document.querySelector("[data-role='overall-percent']");
    var overallProgressFill = document.querySelector("[data-role='overall-progress-fill']");
    var uploadMessage = document.querySelector("[data-role='upload-message']");
    var batchStatus = document.querySelector("[data-role='batch-status']");
    var batchIdValue = document.querySelector("[data-role='batch-id-value']");
    var expectedFileCountValue = document.querySelector("[data-role='expected-file-count-value']");
    var resultSuccessCount = document.querySelector("[data-role='result-success-count']");
    var resultFailCount = document.querySelector("[data-role='result-fail-count']");
    var resultList = document.querySelector("[data-role='result-list']");
    var finalizeResultDetails = document.querySelector("[data-role='finalize-result']");

    if (!targetPathInput || !pickFilesButton || !fileInput || !fileList || !fileEmpty || !resetButton || !startUploadButton) {
      return;
    }

    var selectedFiles = new Map();
    var uploadItems = new Map();

    var batchState = {
      batchId: null,
      phase: "idle",
      finalStatus: null
    };

    var uiState = {
      message: "파일과 경로를 선택하면 업로드를 시작할 수 있습니다.",
      tone: "info"
    };

    var finalizeState = {
      hasResult: false,
      successCount: 0,
      failCount: 0,
      files: []
    };

    var renderQueued = false;

    function getFileKey(file) {
      return file.name + "::" + file.size + "::" + file.lastModified;
    }

    function formatBytes(bytes) {
      if (!Number.isFinite(bytes) || bytes <= 0) {
        return "0 B";
      }

      var units = ["B", "KB", "MB", "GB", "TB"];
      var index = 0;
      var value = bytes;

      while (value >= 1024 && index < units.length - 1) {
        value /= 1024;
        index += 1;
      }

      var fixed = value >= 10 || index === 0 ? 0 : 1;
      return value.toFixed(fixed) + " " + units[index];
    }

    function normalizePath(path) {
      if (!path) {
        return "";
      }

      return path
        .trim()
        .replace(/\\/g, "/")
        .replace(/^\/+/, "")
        .replace(/\/+$/, "");
    }

    function setUploadMessage(message, tone) {
      uiState.message = message;
      uiState.tone = tone;
      requestRender();
    }

    function requestRender() {
      if (renderQueued) {
        return;
      }

      renderQueued = true;
      window.requestAnimationFrame(function () {
        renderQueued = false;
        render();
      });
    }

    function clearFinalizeState() {
      finalizeState.hasResult = false;
      finalizeState.successCount = 0;
      finalizeState.failCount = 0;
      finalizeState.files = [];
    }

    function dispatchFilesChanged() {
      var totalBytes = 0;
      selectedFiles.forEach(function (file) {
        totalBytes += file.size;
      });

      document.dispatchEvent(
        new CustomEvent("transfer:files-changed", {
          detail: {
            fileCount: selectedFiles.size,
            totalBytes: totalBytes,
            files: Array.from(selectedFiles.values())
          }
        })
      );
    }

    function getStatusClass(status) {
      if (status === "업로드중") {
        return "status-pill--uploading";
      }

      if (status === "업로드완료") {
        return "status-pill--uploaded";
      }

      if (status === "저장완료") {
        return "status-pill--stored";
      }

      if (status === "업로드실패" || status === "저장실패") {
        return "status-pill--failed";
      }

      return "status-pill--pending";
    }

    function rebuildUploadItemsFromSelection() {
      uploadItems.clear();

      selectedFiles.forEach(function (file, key) {
        uploadItems.set(key, {
          key: key,
          file: file,
          status: "대기",
          uploadedBytes: 0,
          totalBytes: file.size,
          errorMessage: "",
          upload: null
        });
      });
    }

    function resetBatchStateForEditing() {
      batchState.batchId = null;
      batchState.finalStatus = null;
      batchState.phase = "idle";
      clearFinalizeState();
    }

    function createFileItem(item, canEdit) {
      var li = document.createElement("li");
      li.className = "file-item";

      if (item.status === "업로드중") {
        li.classList.add("file-item--uploading");
      } else if (item.status === "저장완료") {
        li.classList.add("file-item--stored");
      } else if (item.status === "업로드실패" || item.status === "저장실패") {
        li.classList.add("file-item--failed");
      }

      var main = document.createElement("div");
      main.className = "file-item__main";

      var info = document.createElement("div");

      var name = document.createElement("p");
      name.className = "file-item__name";
      name.textContent = item.file.name;

      var meta = document.createElement("p");
      meta.className = "file-item__meta";
      var metaText = formatBytes(item.totalBytes) + " · " + item.status;
      if (item.errorMessage) {
        metaText += " · " + item.errorMessage;
      }

      meta.textContent = metaText;

      info.appendChild(name);
      info.appendChild(meta);

      var actions = document.createElement("div");
      actions.className = "file-item__actions";

      var status = document.createElement("span");
      status.className = "status-pill " + getStatusClass(item.status);
      status.textContent = item.status;

      var remove = document.createElement("button");
      remove.type = "button";
      remove.className = "file-item__remove";
      remove.dataset.role = "remove-selected-file";
      remove.dataset.fileKey = item.key;
      remove.textContent = "제거";
      remove.disabled = !canEdit;

      actions.appendChild(status);
      actions.appendChild(remove);

      main.appendChild(info);
      main.appendChild(actions);

      var track = document.createElement("div");
      track.className = "progress-track progress-track--small";

      var fill = document.createElement("div");
      fill.className = "progress-fill";
      var progress = item.totalBytes > 0 ? Math.min(100, Math.round((item.uploadedBytes / item.totalBytes) * 100)) : 0;
      fill.style.width = progress + "%";

      track.appendChild(fill);

      li.appendChild(main);
      li.appendChild(track);

      return li;
    }

    function setBatchStatusPill(text, styleClass) {
      if (!batchStatus) {
        return;
      }

      batchStatus.classList.remove(
        "status-pill--collecting",
        "status-pill--pending",
        "status-pill--uploading",
        "status-pill--uploaded",
        "status-pill--stored",
        "status-pill--failed"
      );
      batchStatus.classList.add(styleClass);
      batchStatus.textContent = text;
    }

    function renderResultList() {
      if (!resultList) {
        return;
      }

      resultList.innerHTML = "";

      if (!finalizeState.hasResult) {
        var emptyItem = document.createElement("li");
        emptyItem.className = "result-empty-state";
        emptyItem.textContent = "완료 결과가 아직 없습니다.";
        resultList.appendChild(emptyItem);

        if (finalizeResultDetails) {
          finalizeResultDetails.open = false;
        }

        return;
      }

      finalizeState.files.forEach(function (item) {
        var row = document.createElement("li");
        row.className = "result-item";

        var textWrap = document.createElement("div");

        var name = document.createElement("p");
        name.className = "result-item__name";
        name.textContent = item.originalFileName;

        var detail = document.createElement("p");
        detail.className = "result-item__detail";
        if (item.failureReason) {
          detail.textContent = "실패 사유: " + item.failureReason;
        } else if (item.relativePath) {
          detail.textContent = "저장 경로: " + item.relativePath;
        } else {
          detail.textContent = "저장이 완료되었습니다.";
        }

        textWrap.appendChild(name);
        textWrap.appendChild(detail);

        var pill = document.createElement("span");
        if (item.failureReason) {
          pill.className = "status-pill status-pill--failed";
          pill.textContent = "실패";
        } else {
          pill.className = "status-pill status-pill--stored";
          pill.textContent = "저장 완료";
        }

        row.appendChild(textWrap);
        row.appendChild(pill);
        resultList.appendChild(row);
      });

      if (finalizeResultDetails) {
        finalizeResultDetails.open = finalizeState.failCount > 0;
      }
    }

    function render() {
      var canEdit = batchState.phase === "idle" || batchState.phase === "done";
      var normalizedPath = normalizePath(targetPathInput.value);
      var hasTargetPath = normalizedPath.length > 0;
      var hasFiles = selectedFiles.size > 0;

      targetPathInput.disabled = !canEdit;
      if (openPathExplorerButton) {
        openPathExplorerButton.disabled = !canEdit;
      }

      pickFilesButton.disabled = !canEdit;
      resetButton.disabled = !canEdit || !hasFiles;

      startUploadButton.disabled = !tusClientAvailable || !canEdit || !hasFiles || !hasTargetPath;

      if (batchIdValue) {
        batchIdValue.textContent = batchState.batchId ? batchState.batchId : "생성 전";
      }

      if (expectedFileCountValue) {
        expectedFileCountValue.textContent = selectedFiles.size + "건";
      }

      var totalBytes = 0;
      var uploadedBytes = 0;
      var completedItems = 0;
      var uploadingItems = 0;
      var failedItems = 0;

      fileList.innerHTML = "";
      uploadItems.forEach(function (item) {
        totalBytes += item.totalBytes;
        uploadedBytes += item.uploadedBytes;

        if (item.status === "업로드중") {
          uploadingItems += 1;
        }

        if (item.status === "업로드실패" || item.status === "저장실패") {
          failedItems += 1;
        }

        if (item.status === "업로드완료" || item.status === "저장완료") {
          completedItems += 1;
        }

        fileList.appendChild(createFileItem(item, canEdit));
      });

      fileEmpty.hidden = uploadItems.size > 0;

      if (selectedCount) {
        selectedCount.textContent = selectedFiles.size + "건";
      }

      if (selectedSize) {
        selectedSize.textContent = formatBytes(totalBytes);
      }

      if (completedCount) {
        completedCount.textContent = completedItems + "건";
      }

      if (listTotal) {
        listTotal.textContent = "전체 " + selectedFiles.size;
      }

      if (listUploading) {
        listUploading.textContent = "업로드 중 " + uploadingItems;
      }

      if (listFailed) {
        listFailed.textContent = "실패 " + failedItems;
      }

      var percent = totalBytes > 0 ? Math.min(100, Math.round((uploadedBytes / totalBytes) * 100)) : 0;
      if (overallPercent) {
        overallPercent.textContent = percent + "%";
      }

      if (overallProgressFill) {
        overallProgressFill.style.width = percent + "%";
      }

      if (uploadMessage) {
        uploadMessage.textContent = uiState.message;
        uploadMessage.classList.remove(
          "upload-message--info",
          "upload-message--success",
          "upload-message--warn",
          "upload-message--error"
        );
        uploadMessage.classList.add("upload-message--" + uiState.tone);
      }

      if (batchState.phase === "idle") {
        setBatchStatusPill("수집 중", "status-pill--collecting");
      } else if (batchState.phase === "creating" || batchState.phase === "uploading") {
        setBatchStatusPill("업로드 중", "status-pill--uploading");
      } else if (batchState.phase === "finalizing") {
        setBatchStatusPill("마무리 중", "status-pill--uploaded");
      } else if (batchState.finalStatus === 1) {
        setBatchStatusPill("완료", "status-pill--stored");
      } else if (batchState.finalStatus === 2) {
        setBatchStatusPill("부분 완료", "status-pill--uploaded");
      } else {
        setBatchStatusPill("실패", "status-pill--failed");
      }

      if (resultSuccessCount) {
        resultSuccessCount.textContent = finalizeState.successCount + "건";
      }

      if (resultFailCount) {
        resultFailCount.textContent = finalizeState.failCount + "건";
      }

      renderResultList();
    }

    async function readProblemMessage(response, fallbackMessage) {
      try {
        var payload = await response.json();
        if (payload && (payload.detail || payload.title)) {
          return payload.detail || payload.title;
        }
      } catch (_ignored) {
      }

      return fallbackMessage;
    }

    async function createTransferBatch(targetPath, expectedFileCount) {
      var response = await fetch("/api/transfer/batches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          targetPath: targetPath,
          expectedFileCount: expectedFileCount
        })
      });

      if (!response.ok) {
        var createErrorMessage = await readProblemMessage(response, "업로드 배치 생성에 실패했습니다.");
        throw { userMessage: createErrorMessage };
      }

      return response.json();
    }

    async function finalizeTransferBatch(batchId) {
      var response = await fetch("/api/transfer/batches/" + encodeURIComponent(batchId) + "/complete", {
        method: "POST"
      });

      if (!response.ok) {
        var finalizeErrorMessage = await readProblemMessage(response, "서버 마무리 처리에 실패했습니다.");
        throw { userMessage: finalizeErrorMessage };
      }

      return response.json();
    }

    function extractTusErrorMessage(error) {
      if (error && error.originalResponse && typeof error.originalResponse.getBody === "function") {
        try {
          var body = error.originalResponse.getBody();
          if (body) {
            var payload = JSON.parse(body);
            if (payload && (payload.detail || payload.title)) {
              return payload.detail || payload.title;
            }
          }
        } catch (_ignored) {
        }
      }

      return "파일 업로드에 실패했습니다.";
    }

    function uploadSingleFile(item, targetPath, batchId) {
      return new Promise(function (resolve) {
        item.status = "업로드중";
        item.errorMessage = "";
        item.uploadedBytes = 0;
        requestRender();

        var upload = new tus.Upload(item.file, {
          endpoint: "/api/transfer/uploads",
          chunkSize: 5 * 1024 * 1024,
          retryDelays: [0, 1000, 3000, 5000],
          metadata: {
            targetPath: targetPath,
            fileName: item.file.name,
            batchId: String(batchId),
            contentType: item.file.type || "application/octet-stream"
          },
          onError: function (error) {
            item.status = "업로드실패";
            item.errorMessage = extractTusErrorMessage(error);
            item.upload = null;
            requestRender();
            resolve();
          },
          onProgress: function (bytesUploaded, bytesTotal) {
            item.uploadedBytes = bytesUploaded;
            item.totalBytes = bytesTotal || item.totalBytes;
            requestRender();
          },
          onSuccess: function () {
            item.uploadedBytes = item.totalBytes;
            item.status = "업로드완료";
            item.upload = null;
            requestRender();
            resolve();
          }
        });

        item.upload = upload;

        try {
          upload.start();
        } catch (_startError) {
          item.status = "업로드실패";
          item.errorMessage = "파일 업로드 시작에 실패했습니다.";
          item.upload = null;
          requestRender();
          resolve();
        }
      });
    }

    async function runUploadQueue(targetPath, batchId, parallelCount) {
      var items = Array.from(uploadItems.values());
      var nextIndex = 0;

      async function worker() {
        while (true) {
          var currentIndex = nextIndex;
          nextIndex += 1;

          if (currentIndex >= items.length) {
            return;
          }

          await uploadSingleFile(items[currentIndex], targetPath, batchId);
        }
      }

      var workerCount = Math.min(parallelCount, items.length);
      var workers = [];
      for (var i = 0; i < workerCount; i += 1) {
        workers.push(worker());
      }

      await Promise.all(workers);
    }

    function applyFinalizeResult(finalizeResult) {
      finalizeState.hasResult = true;
      finalizeState.files = Array.isArray(finalizeResult.files) ? finalizeResult.files : [];
      finalizeState.successCount = 0;
      finalizeState.failCount = 0;

      var bucket = new Map();
      uploadItems.forEach(function (item, key) {
        var bucketKey = item.file.name + "::" + item.file.size;
        if (!bucket.has(bucketKey)) {
          bucket.set(bucketKey, []);
        }

        bucket.get(bucketKey).push(key);
      });

      finalizeState.files.forEach(function (fileResult) {
        var bucketKey = fileResult.originalFileName + "::" + fileResult.sizeBytes;
        var keys = bucket.get(bucketKey);
        var matchedKey = keys && keys.length > 0 ? keys.shift() : null;

        if (matchedKey && uploadItems.has(matchedKey)) {
          var uploadItem = uploadItems.get(matchedKey);
          if (fileResult.failureReason) {
            uploadItem.status = "저장실패";
            uploadItem.errorMessage = fileResult.failureReason;
          } else {
            uploadItem.status = "저장완료";
            uploadItem.errorMessage = "";
            uploadItem.uploadedBytes = uploadItem.totalBytes;
          }
        }

        if (fileResult.failureReason) {
          finalizeState.failCount += 1;
        } else {
          finalizeState.successCount += 1;
        }
      });

      uploadItems.forEach(function (uploadItem) {
        if (uploadItem.status === "업로드실패") {
          finalizeState.failCount += 1;

          finalizeState.files.push({
            originalFileName: uploadItem.file.name,
            storedFileName: null,
            relativePath: null,
            sizeBytes: uploadItem.totalBytes,
            failureReason: uploadItem.errorMessage || "파일 업로드에 실패했습니다."
          });
        }
      });
    }

    async function startUploadWorkflow() {
      if (batchState.phase !== "idle" && batchState.phase !== "done") {
        return;
      }

      if (!tusClientAvailable) {
        setUploadMessage("업로드 라이브러리를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.", "error");
        return;
      }

      var targetPath = normalizePath(targetPathInput.value);
      if (!targetPath) {
        setUploadMessage("대상 경로를 입력해 주세요.", "error");
        return;
      }

      if (selectedFiles.size === 0) {
        setUploadMessage("업로드할 파일을 선택해 주세요.", "error");
        return;
      }

      batchState.phase = "creating";
      batchState.batchId = null;
      batchState.finalStatus = null;
      clearFinalizeState();
      rebuildUploadItemsFromSelection();
      setUploadMessage("업로드 배치를 생성하는 중입니다.", "info");
      requestRender();

      try {
        var batch = await createTransferBatch(targetPath, selectedFiles.size);
        batchState.batchId = batch.batchId;

        batchState.phase = "uploading";
        setUploadMessage("파일 업로드를 시작합니다.", "info");
        requestRender();

        await runUploadQueue(targetPath, batch.batchId, 2);

        batchState.phase = "finalizing";
        setUploadMessage("서버 마무리 처리를 진행 중입니다.", "info");
        requestRender();

        var finalizeResult = await finalizeTransferBatch(batch.batchId);
        applyFinalizeResult(finalizeResult);

        batchState.phase = "done";
        batchState.finalStatus = finalizeResult.status;

        if (finalizeResult.status === 1) {
          setUploadMessage("업로드와 저장이 완료되었습니다.", "success");
        } else if (finalizeResult.status === 2) {
          setUploadMessage("일부 파일 저장에 실패했습니다. 결과를 확인해 주세요.", "warn");
        } else {
          setUploadMessage("배치 처리에 실패했습니다. 결과를 확인해 주세요.", "error");
        }
      } catch (error) {
        batchState.phase = "done";
        batchState.finalStatus = 3;

        var message = error && error.userMessage
          ? error.userMessage
          : "업로드 처리 중 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.";

        setUploadMessage(message, "error");
      }

      requestRender();
    }

    function prepareForFileSelectionChange() {
      if (batchState.phase === "uploading" || batchState.phase === "finalizing") {
        return false;
      }

      resetBatchStateForEditing();
      rebuildUploadItemsFromSelection();
      if (!tusClientAvailable) {
        setUploadMessage("업로드 라이브러리를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.", "error");
      } else {
        setUploadMessage("파일과 경로를 선택하면 업로드를 시작할 수 있습니다.", "info");
      }

      return true;
    }

    function addFiles(fileCollection) {
      Array.from(fileCollection).forEach(function (file) {
        var key = getFileKey(file);
        if (!selectedFiles.has(key)) {
          selectedFiles.set(key, file);
        }
      });

      if (!prepareForFileSelectionChange()) {
        return;
      }

      dispatchFilesChanged();
      requestRender();
    }

    pickFilesButton.addEventListener("click", function () {
      if (batchState.phase === "uploading" || batchState.phase === "finalizing") {
        return;
      }

      fileInput.click();
    });

    fileInput.addEventListener("change", function () {
      if (fileInput.files && fileInput.files.length > 0) {
        addFiles(fileInput.files);
      }

      fileInput.value = "";
    });

    targetPathInput.addEventListener("input", function () {
      requestRender();
    });

    startUploadButton.addEventListener("click", function () {
      startUploadWorkflow();
    });

    resetButton.addEventListener("click", function () {
      if (batchState.phase === "uploading" || batchState.phase === "finalizing") {
        return;
      }

      selectedFiles.clear();
      resetBatchStateForEditing();
      rebuildUploadItemsFromSelection();
      dispatchFilesChanged();

      if (!tusClientAvailable) {
        setUploadMessage("업로드 라이브러리를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.", "error");
      } else {
        setUploadMessage("파일과 경로를 선택하면 업로드를 시작할 수 있습니다.", "info");
      }

      requestRender();
    });

    fileList.addEventListener("click", function (event) {
      if (batchState.phase === "uploading" || batchState.phase === "finalizing") {
        return;
      }

      var button = event.target.closest("button[data-role='remove-selected-file']");
      if (!button) {
        return;
      }

      var key = button.dataset.fileKey;
      if (!key || !selectedFiles.has(key)) {
        return;
      }

      selectedFiles.delete(key);
      resetBatchStateForEditing();
      rebuildUploadItemsFromSelection();
      dispatchFilesChanged();

      if (!tusClientAvailable) {
        setUploadMessage("업로드 라이브러리를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.", "error");
      } else {
        setUploadMessage("파일과 경로를 선택하면 업로드를 시작할 수 있습니다.", "info");
      }

      requestRender();
    });

    rebuildUploadItemsFromSelection();

    if (!tusClientAvailable) {
      setUploadMessage("업로드 라이브러리를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.", "error");
    } else {
      requestRender();
    }
  });
})();
