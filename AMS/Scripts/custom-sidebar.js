"use strict";
$(document).ready(function () {
    var gullUtils = {
        isMobile: function () {
            return window.matchMedia("(max-width: 767px)").matches;
        }
        // Add other utility functions as needed
    };
    var sidebarContainers = $("[data-sidebar-container]").addClass("sidebar-container");
    $("[data-sidebar-content]").addClass("sidebar-content");
    $("[data-sidebar]").addClass("sidebar");

    sidebarContainers.each(function () {
        var currentContainer = $(this);
        var sidebarContent, sidebar, sidebarToggle, sidebarPosition, sidebarWidth;

        function setSidebarProperties() {
            sidebarContent = $('[data-sidebar-content="' + currentContainer.data("sidebar-container") + '"]');
            sidebar = $('[data-sidebar="' + currentContainer.data("sidebar-container") + '"]');
            sidebarToggle = $('[data-sidebar-toggle="' + currentContainer.data("sidebar-container") + '"]');
            sidebarPosition = sidebar.data("sidebar-position");
            sidebarWidth = sidebar.outerWidth();

            if (sidebarPosition === "right") {
                if (gullUtils.isMobile()) {
                    sidebarContent.css("margin-right", 0);
                    sidebar.css("right", -sidebarWidth);
                } else {
                    sidebarContent.css("margin-right", sidebarWidth);
                    sidebar.css("right", 0);
                }
            } else {
                if (gullUtils.isMobile()) {
                    sidebarContent.css("margin-left", 0);
                    sidebar.css("left", -sidebarWidth);
                } else {
                    sidebarContent.css("margin-left", sidebarWidth);
                    sidebar.css("left", 0);
                }
            }
        }

        setSidebarProperties();

        $(window).on("resize", function () {
            setTimeout(function () {
                setSidebarProperties();
            }, 300);
        });

        sidebarToggle.on("click", function () {
            if (sidebarPosition === "right") {
                if (sidebar.css("right") === "0px") {
                    sidebar.css("right", -sidebarWidth);
                    if (!gullUtils.isMobile()) {
                        sidebarContent.css("margin-right", 0);
                    }
                } else {
                    sidebar.css("right", 0);
                    if (!gullUtils.isMobile()) {
                        sidebarContent.css("margin-right", sidebarWidth);
                    }
                }
            } else {
                if (sidebar.css("left") === "0px") {
                    sidebar.css("left", -sidebarWidth);
                    if (!gullUtils.isMobile()) {
                        sidebarContent.css("margin-left", 0);
                    }
                } else {
                    sidebar.css("left", 0);
                    if (!gullUtils.isMobile()) {
                        sidebarContent.css("margin-left", sidebarWidth);
                    }
                }
            }
        });

        
    });
});
