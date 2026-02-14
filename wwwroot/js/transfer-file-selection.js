(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    var pickFilesButton = document.querySelector("[data-role='pick-files']");
    var fileInput = document.querySelector("[data-role='file-input']");
    var fileList = document.querySelector("[data-role='selected-file-list']");
    var fileEmpty = document.querySelector("[data-role='file-empty']");
    var resetButton = document.querySelector("[data-role='reset-upload']");
    var startUploadButton = document.querySelector("[data-role='start-upload']");
    var selectedCount = document.querySelector("[data-role='selected-count']");
    var selectedSize = document.querySelector("[data-role='selected-size']");
    var filePickerHint = document.querySelector("[data-role='file-picker-hint']");
    var listTotal = document.querySelector("[data-role='list-total']");
    var listUploading = document.querySelector("[data-role='list-uploading']");
    var listFailed = document.querySelector("[data-role='list-failed']");

    if (!pickFilesButton || !fileInput || !fileList || !fileEmpty || !resetButton) {
      return;
    }

    var selectedFiles = new Map();

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

    function dispatchFilesChanged() {
      var totalBytes = 0;
      selectedFiles.forEach(function (file) {
        totalBytes += file.size;
      });

      document.dispatchEvent(
        new CustomEvent("transfer:files-changed", {
          detail: {
            fileCount: selectedFiles.size,
            totalBytes: totalBytes
          }
        })
      );
    }

    function createFileItem(file, key) {
      var item = document.createElement("li");
      item.className = "file-item";

      var main = document.createElement("div");
      main.className = "file-item__main";

      var info = document.createElement("div");

      var name = document.createElement("p");
      name.className = "file-item__name";
      name.textContent = file.name;

      var meta = document.createElement("p");
      meta.className = "file-item__meta";
      meta.textContent = formatBytes(file.size) + " · 대기";

      info.appendChild(name);
      info.appendChild(meta);

      var actions = document.createElement("div");
      actions.className = "file-item__actions";

      var status = document.createElement("span");
      status.className = "status-pill status-pill--pending";
      status.textContent = "대기";

      var remove = document.createElement("button");
      remove.type = "button";
      remove.className = "file-item__remove";
      remove.dataset.role = "remove-selected-file";
      remove.dataset.fileKey = key;
      remove.textContent = "제거";

      actions.appendChild(status);
      actions.appendChild(remove);

      main.appendChild(info);
      main.appendChild(actions);

      var progressTrack = document.createElement("div");
      progressTrack.className = "progress-track progress-track--small";

      var progressFill = document.createElement("div");
      progressFill.className = "progress-fill";
      progressFill.style.width = "0%";

      progressTrack.appendChild(progressFill);

      item.appendChild(main);
      item.appendChild(progressTrack);

      return item;
    }

    function render() {
      var totalBytes = 0;
      fileList.innerHTML = "";

      selectedFiles.forEach(function (file, key) {
        totalBytes += file.size;
        fileList.appendChild(createFileItem(file, key));
      });

      var fileCount = selectedFiles.size;
      var hasFiles = fileCount > 0;

      fileEmpty.hidden = hasFiles;

      if (selectedCount) {
        selectedCount.textContent = fileCount + "건";
      }

      if (selectedSize) {
        selectedSize.textContent = formatBytes(totalBytes);
      }

      if (listTotal) {
        listTotal.textContent = "전체 " + fileCount;
      }

      if (listUploading) {
        listUploading.textContent = "업로드 중 0";
      }

      if (listFailed) {
        listFailed.textContent = "실패 0";
      }

      if (filePickerHint) {
        filePickerHint.textContent = hasFiles ? fileCount + "개 파일 선택됨" : "선택된 파일 없음";
      }

      resetButton.disabled = !hasFiles;
      if (startUploadButton) {
        startUploadButton.disabled = true;
      }

      dispatchFilesChanged();
    }

    function addFiles(fileListLike) {
      Array.from(fileListLike).forEach(function (file) {
        var key = getFileKey(file);
        if (!selectedFiles.has(key)) {
          selectedFiles.set(key, file);
        }
      });

      render();
    }

    pickFilesButton.addEventListener("click", function () {
      fileInput.click();
    });

    fileInput.addEventListener("change", function () {
      if (fileInput.files && fileInput.files.length > 0) {
        addFiles(fileInput.files);
      }

      fileInput.value = "";
    });

    resetButton.addEventListener("click", function () {
      selectedFiles.clear();
      render();
    });

    fileList.addEventListener("click", function (event) {
      var button = event.target.closest("button[data-role='remove-selected-file']");
      if (!button) {
        return;
      }

      var fileKey = button.dataset.fileKey;
      if (!fileKey) {
        return;
      }

      selectedFiles.delete(fileKey);
      render();
    });

    render();
  });
})();
