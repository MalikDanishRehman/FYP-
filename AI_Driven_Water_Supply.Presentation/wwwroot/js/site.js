window.toggleSidePanel = function () {
    let side = document.getElementById("sidePanel");
    let body = document.querySelector(".dashboard-container");

    if (side.classList.contains("side-panel-active")) {
        side.classList.remove("side-panel-active");
        body.classList.remove("body-shrink");
    } else {
        side.classList.add("side-panel-active");
        body.classList.add("body-shrink");
    }
};
