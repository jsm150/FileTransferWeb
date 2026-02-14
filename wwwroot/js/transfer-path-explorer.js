(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    var targetPathInput = document.querySelector("[data-role='target-path']");
    var openButton = document.querySelector("[data-role='open-path-explorer']");
    var modal = document.querySelector("[data-role='path-explorer-modal']");

    if (!targetPathInput || !openButton || !modal) {
      return;
    }

    var closeButton = modal.querySelector("[data-role='close-path-explorer']");
    var cancelButton = modal.querySelector("[data-role='cancel-path-explorer']");
    var selectCurrentButton = modal.querySelector("[data-role='select-current-path']");
    var breadcrumb = modal.querySelector("[data-role='path-breadcrumb']");
    var directoryList = modal.querySelector("[data-role='directory-list']");
    var currentPathText = modal.querySelector("[data-role='path-current-text']");
    var loadingBox = modal.querySelector("[data-role='path-loading']");
    var errorBox = modal.querySelector("[data-role='path-error']");
    var emptyBox = modal.querySelector("[data-role='path-empty']");
    var retryButton = modal.querySelector("[data-role='retry-path-load']");
    var goRootButton = modal.querySelector("[data-role='go-path-root']");
    var recoveryActions = modal.querySelector("[data-role='path-recovery-actions']");

    var state = {
      currentPath: "",
      parentPath: null,
      directories: [],
      isLoading: false,
      errorMessage: "",
      lastRequestedPath: ""
    };

    var modalOpen = false;
    var previousFocusedElement = null;

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

    function getDisplayPath(path) {
      return path ? "/" + path : "/";
    }

    function buildDirectoryApiUrl(relativePath) {
      var normalized = normalizePath(relativePath);
      if (!normalized) {
        return "/api/storage/directories";
      }

      var params = new URLSearchParams({ relativePath: normalized });
      return "/api/storage/directories?" + params.toString();
    }

    function getApiErrorMessage(status, fallbackMessage) {
      if (status === 400) {
        return fallbackMessage || "요청한 경로가 올바르지 않습니다.";
      }

      if (status === 404) {
        return fallbackMessage || "요청한 폴더를 찾을 수 없습니다.";
      }

      return fallbackMessage || "폴더 목록을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.";
    }

    async function fetchDirectories(relativePath) {
      var response = await fetch(buildDirectoryApiUrl(relativePath), {
        method: "GET",
        headers: {
          Accept: "application/json"
        }
      });

      if (!response.ok) {
        var fallbackMessage = "";

        try {
          var problem = await response.json();
          fallbackMessage = problem && (problem.detail || problem.title) ? (problem.detail || problem.title) : "";
        } catch (_ignored) {
          fallbackMessage = "";
        }

        var apiError = new Error(getApiErrorMessage(response.status, fallbackMessage));
        apiError.status = response.status;
        throw apiError;
      }

      return response.json();
    }

    function createBreadcrumbButton(label, path) {
      var item = document.createElement("span");
      item.className = "path-breadcrumb__item";

      var button = document.createElement("button");
      button.type = "button";
      button.className = "path-breadcrumb__button";
      button.dataset.pathNav = path;
      button.textContent = label;

      item.appendChild(button);
      return item;
    }

    function renderBreadcrumb(path) {
      breadcrumb.innerHTML = "";

      var normalized = normalizePath(path);
      var segments = normalized ? normalized.split("/") : [];

      breadcrumb.appendChild(createBreadcrumbButton("루트", ""));

      var current = "";
      for (var i = 0; i < segments.length; i += 1) {
        var divider = document.createElement("span");
        divider.className = "path-breadcrumb__divider";
        divider.textContent = ">";
        breadcrumb.appendChild(divider);

        current = current ? current + "/" + segments[i] : segments[i];
        breadcrumb.appendChild(createBreadcrumbButton(segments[i], current));
      }
    }

    function applyPathSelection(path) {
      targetPathInput.value = normalizePath(path);
      targetPathInput.dispatchEvent(new Event("input", { bubbles: true }));
    }

    function createPathItem(name, relativePath, meta, openLabel) {
      var item = document.createElement("li");
      item.className = "path-item";

      var textWrap = document.createElement("div");

      var title = document.createElement("p");
      title.className = "path-item__name";
      title.textContent = name;

      var metaText = document.createElement("p");
      metaText.className = "path-item__meta";
      metaText.textContent = meta;

      textWrap.appendChild(title);
      textWrap.appendChild(metaText);

      var actions = document.createElement("div");
      actions.className = "path-item__actions";

      var open = document.createElement("button");
      open.type = "button";
      open.className = "path-item__open";
      open.dataset.pathOpen = normalizePath(relativePath);
      open.textContent = openLabel;

      var select = document.createElement("button");
      select.type = "button";
      select.className = "path-item__select";
      select.dataset.pathSelect = normalizePath(relativePath);
      select.textContent = "선택";

      actions.appendChild(open);
      actions.appendChild(select);

      item.appendChild(textWrap);
      item.appendChild(actions);

      return item;
    }

    function renderDirectoryList() {
      directoryList.innerHTML = "";

      var hasParent = typeof state.parentPath === "string";
      if (hasParent) {
        directoryList.appendChild(createPathItem("..", state.parentPath, "상위 폴더로 이동", "이동"));
      }

      state.directories.forEach(function (directory) {
        directoryList.appendChild(
          createPathItem(directory.name, directory.relativePath, "하위 폴더", "열기")
        );
      });

      var hasDirectories = directoryList.children.length > 0;
      emptyBox.hidden = hasDirectories || state.isLoading || !!state.errorMessage;
    }

    function renderState() {
      currentPathText.textContent = getDisplayPath(state.currentPath);
      loadingBox.hidden = !state.isLoading;

      if (state.errorMessage) {
        errorBox.hidden = false;
        errorBox.textContent = state.errorMessage;
        recoveryActions.hidden = false;
      } else {
        errorBox.hidden = true;
        errorBox.textContent = "";
        recoveryActions.hidden = true;
      }

      selectCurrentButton.disabled = state.isLoading || !!state.errorMessage;

      renderBreadcrumb(state.currentPath);
      renderDirectoryList();
    }

    async function loadDirectories(relativePath) {
      var requested = normalizePath(relativePath);
      state.lastRequestedPath = requested;
      state.isLoading = true;
      state.errorMessage = "";
      renderState();

      try {
        var data = await fetchDirectories(requested);

        state.currentPath = normalizePath(data.currentPath);
        state.parentPath = data.parentPath == null ? null : normalizePath(data.parentPath);
        state.directories = Array.isArray(data.directories)
          ? data.directories.map(function (directory) {
            return {
              name: directory && directory.name ? String(directory.name) : "이름 없는 폴더",
              relativePath: directory && directory.relativePath ? normalizePath(directory.relativePath) : ""
            };
          })
          : [];

        return true;
      } catch (error) {
        state.currentPath = requested;
        state.parentPath = null;
        state.directories = [];
        state.errorMessage = error && error.message
          ? error.message
          : "폴더 목록을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.";

        return false;
      } finally {
        state.isLoading = false;
        renderState();
      }
    }

    function getFocusableElements() {
      var selectors = [
        "button:not([disabled])",
        "[href]",
        "input:not([disabled])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "[tabindex]:not([tabindex='-1'])"
      ];

      return Array.prototype.slice
        .call(modal.querySelectorAll(selectors.join(",")))
        .filter(function (element) {
          return !element.hasAttribute("hidden") && element.offsetParent !== null;
        });
    }

    function closeExplorer(options) {
      if (!modalOpen) {
        return;
      }

      var shouldApply = options && options.applySelection === true;
      if (shouldApply) {
        applyPathSelection(state.currentPath);
      }

      modal.hidden = true;
      modalOpen = false;
      document.body.classList.remove("path-explorer-open");

      if (previousFocusedElement && typeof previousFocusedElement.focus === "function") {
        previousFocusedElement.focus();
      }
    }

    async function openExplorer() {
      previousFocusedElement = document.activeElement;
      modal.hidden = false;
      modalOpen = true;
      document.body.classList.add("path-explorer-open");

      var initialPath = normalizePath(targetPathInput.value);
      var loaded = await loadDirectories(initialPath);

      if (!loaded && initialPath) {
        await loadDirectories("");
      }

      if (closeButton) {
        closeButton.focus();
      }
    }

    function onDocumentKeyDown(event) {
      if (!modalOpen) {
        return;
      }

      if (event.key === "Escape") {
        event.preventDefault();
        closeExplorer({ applySelection: false });
        return;
      }

      if (event.key !== "Tab") {
        return;
      }

      var focusable = getFocusableElements();
      if (focusable.length === 0) {
        event.preventDefault();
        return;
      }

      var first = focusable[0];
      var last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }

    openButton.addEventListener("click", function () {
      openExplorer();
    });

    if (closeButton) {
      closeButton.addEventListener("click", function () {
        closeExplorer({ applySelection: true });
      });
    }

    if (cancelButton) {
      cancelButton.addEventListener("click", function () {
        closeExplorer({ applySelection: false });
      });
    }

    modal.addEventListener("click", function (event) {
      if (event.target === modal) {
        closeExplorer({ applySelection: false });
      }
    });

    selectCurrentButton.addEventListener("click", function () {
      closeExplorer({ applySelection: true });
    });

    breadcrumb.addEventListener("click", function (event) {
      var button = event.target.closest("button[data-path-nav]");
      if (!button) {
        return;
      }

      loadDirectories(button.dataset.pathNav || "");
    });

    directoryList.addEventListener("click", function (event) {
      var button = event.target.closest("button[data-path-open]");
      if (!button) {
        button = event.target.closest("button[data-path-select]");
        if (!button) {
          return;
        }

        applyPathSelection(button.dataset.pathSelect || "");
        closeExplorer({ applySelection: false });
        return;
      }

      loadDirectories(button.dataset.pathOpen || "");
    });

    retryButton.addEventListener("click", function () {
      loadDirectories(state.lastRequestedPath || state.currentPath || "");
    });

    goRootButton.addEventListener("click", function () {
      loadDirectories("");
    });

    document.addEventListener("keydown", onDocumentKeyDown);
  });
})();
