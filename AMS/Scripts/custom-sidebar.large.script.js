"use strict";
$(document).ready(function () {
    var gullUtils = {
    isMobile: function () {
        return window.matchMedia("(max-width: 767px)").matches;
    }
    // Add other utility functions as needed
};
    var e = $(".menu-toggle"),
        t = $(".sidebar-left"),
        s = $(".sidebar-left-secondary"),
        a = $(".sidebar-overlay"),
        n = $(".main-content-wrap"),
        i = $(".nav-item");

    function o() {
        t.addClass("open"), n.addClass("sidenav-open")
    }

    function l() {
        t.removeClass("open"), n.removeClass("sidenav-open")
    }

    function c() {
        s.addClass("open"), a.addClass("open")
    }

    function d() {
        s.removeClass("open"), a.removeClass("open")
    }

    $(window).on("resize", function () {
        gullUtils.isMobile() && (l(), d())
    });

    i.each(function () {
        var currentItem = $(this);
        if (currentItem.hasClass("active")) {
            currentItem = currentItem.data("item");
            s.find('[data-parent="' + currentItem + '"]').show();
        }
    });

    gullUtils.isMobile() && (l(), d());

    t.find(".nav-item").on("mouseenter", function () {
        var currentItem = $(this).data("item");
        $(".nav-item").removeClass("active");
        $(this).addClass("active");
        c();
        s.find(".childNav").hide();
        s.find('[data-parent="' + currentItem + '"]').show();
    });

    t.find(".nav-item").on("click", function (event) {
        if ($(event.currentTarget).data("item")) {
            event.preventDefault();
        }
    });

    a.on("click", function () {
        gullUtils.isMobile() && l(), d();
    });

    e.on("click", function () {
        var isSidebarOpen = t.hasClass("open");
        var isSidebarSecondaryOpen = s.hasClass("open");
        var activeNavItem = $(".nav-item.active").data("item");

        if (isSidebarOpen && isSidebarSecondaryOpen && gullUtils.isMobile()) {
            l();
            d();
        } else if (isSidebarOpen && isSidebarSecondaryOpen) {
            d();
        } else if (isSidebarOpen) {
            l();
        } else if (!isSidebarSecondaryOpen && activeNavItem) {
            o();
            c();
        } else {
            o();
        }
    });
});
