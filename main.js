const typedTextSpan = document.querySelector(".typed-text");
const cursorSpan = document.querySelector(".cursor");

const textArray = ["security", "auto mod", "verification", "protection", "monitoring", "anti raid", "blocking fake accounts", "blocking spam accounts", "keeping your server safe"];
const typingDelay = 100;
const erasingDelay = 100;
const newTextDelay = 700; // Delay between current and next text
let textArrayIndex = 0;
let charIndex = 0;

function type() {
    if (charIndex < textArray[textArrayIndex].length) {
        if (!cursorSpan.classList.contains("typing")) cursorSpan.classList.add("typing");
        typedTextSpan.textContent += textArray[textArrayIndex].charAt(charIndex);
        charIndex++;
        setTimeout(type, typingDelay);
    } else {
        cursorSpan.classList.remove("typing");
        setTimeout(erase, newTextDelay);
    }
}

function erase() {
    if (charIndex > 0) {
        if (!cursorSpan.classList.contains("typing")) cursorSpan.classList.add("typing");
        typedTextSpan.textContent = textArray[textArrayIndex].substring(0, charIndex - 1);
        charIndex--;
        setTimeout(erase, erasingDelay);
    } else {
        cursorSpan.classList.remove("typing");
        textArrayIndex++;
        if (textArrayIndex >= textArray.length) textArrayIndex = 0;
        setTimeout(type, typingDelay + 1100);
    }
}

document.addEventListener("DOMContentLoaded", function () { // On DOM Load initiate the effect
    if (textArray.length) setTimeout(type, newTextDelay + 250);
});

$(document).ready(function () {
    $(".open").click(function () {
        const container = $(this).parents(".topic");
        const answer = container.find(".answer");
        const trigger = container.find(".faq-t");


        answer.slideToggle(100);

        if (trigger.hasClass("faq-o")) {
            trigger.removeClass("faq-o");
        } else {
            trigger.addClass("faq-o");
        }

        if (container.hasClass("expanded")) {
            container.removeClass("expanded");
        } else {
            container.addClass("expanded");
        }
    });
    $(".question").each(function () {
        $(this).attr(
            "data-search-term",
            $(this).text().toLowerCase() + $(this).find("ptag").text().toLowerCase()
        );
    });

    $(".live-search-box").on("keyup", function () {
        const searchTerm = $(this).val().toLowerCase();

        $(".question").each(function () {
            if (
                $(this).filter("[data-search-term *= " + searchTerm + "]").length > 0 ||
                searchTerm.length < 1
            ) {
                $(this).parent().parent().show();
            } else {
                $(this).parent().parent().hide();
            }
        });
    });
});
window.addEventListener("scroll", reveal);

function reveal() {
    const reveals = document.querySelectorAll(".reveal");

    for (let i = 0; i < reveals.length; i++) {
        const windowheight = window.innerHeight;
        const revealtop = reveals[i].getBoundingClientRect().top;
        const revealpoint = 150;

        if (revealtop < windowheight - revealpoint) {
            reveals[i].classList.add("active");
        } else {
        }
    }
}

reveals[i].classList.remove("active");