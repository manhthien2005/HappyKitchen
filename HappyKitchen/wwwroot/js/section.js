document.addEventListener("DOMContentLoaded", function () {
  const sections = document.querySelectorAll("section"); 
  const navLinks = document.querySelectorAll(".navbar-link");

  function activateMenu() {
      let scrollY = window.scrollY;

      sections.forEach((section) => {
          const sectionTop = section.offsetTop - 100; // Điều chỉnh nếu cần để khớp header
          const sectionHeight = section.offsetHeight;
          const sectionId = section.getAttribute("id");

          if (scrollY >= sectionTop && scrollY < sectionTop + sectionHeight) {
              navLinks.forEach((link) => {
                  link.classList.remove("active");
                  if (link.getAttribute("href") === `#${sectionId}`) {
                      link.classList.add("active");
                  }
              });
          }
      });
  }

  window.addEventListener("scroll", activateMenu);
});

