document.addEventListener("DOMContentLoaded", () => {

  document.querySelectorAll(".password-toggle").forEach((toggle) => {
    toggle.addEventListener("click", (event) => {
      event.preventDefault();
      event.stopPropagation();
      const input = toggle.parentElement.querySelector("input");
      const isHidden = input.type === "password";
      input.type = isHidden ? "text" : "password";
      toggle.setAttribute("aria-label", isHidden ? "Hide password" : "Show password");
      toggle.innerHTML = isHidden
        ? '<svg class="eye-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 5.2A10.8 10.8 0 0 1 12 5c6.5 0 10 7 10 7a18.4 18.4 0 0 1-3.1 3.8M6.2 6.2C3.5 8 2 12 2 12s3.5 7 10 7a10.6 10.6 0 0 0 4-.8"></path></svg>'
        : '<svg class="eye-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z"></path><circle cx="12" cy="12" r="2.5"></circle></svg>';
    });
  });

  // Full name: letters and spaces only
  const fullNameInput = document.getElementById("fullName");
  if (fullNameInput) {
    fullNameInput.addEventListener("input", (e) => {
      e.target.value = e.target.value.replace(/[^a-zA-Z\s]/g, "");
    });
  }

  // Phone: digits only, max 10
  const phoneInput = document.getElementById("phone");
  if (phoneInput) {
    phoneInput.addEventListener("input", (e) => {
      e.target.value = e.target.value.replace(/[^0-9]/g, "").slice(0, 10);
    });
  }

  // Email: letters, numbers, @ and . only
  const emailInput = document.getElementById("email");
  if (emailInput) {
    emailInput.addEventListener("input", (e) => {
      e.target.value = e.target.value.replace(/[^a-zA-Z0-9@.]/g, "");
    });
  }

  // Signup form submission
  const signupForm = document.getElementById("signupForm");
  if (signupForm) {
    signupForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      const formData = Object.fromEntries(new FormData(signupForm).entries());
      const payload = {
        ...formData,
        fullName: String(formData.fullName || "").trim(),
        email: String(formData.email || "").trim(),
        phone: String(formData.phone || "").trim(),
        role: normalizeRole(formData.role)
      };

      try {
        const res = await fetch("/api/auth/signup", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (!res.ok) {
          document.getElementById("signupMessage").textContent = data.message || "Signup failed";
          return;
        }
        localStorage.setItem("eduConnectUser", JSON.stringify(data));
        redirectToDashboard(data.role);
      } catch (err) {
        document.getElementById("signupMessage").textContent = "Something went wrong.";
        console.error(err);
      }
    });
  }

  // Login form submission
  const loginForm = document.getElementById("loginForm");
  if (loginForm) {
    loginForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      const payload = {
        email: String(new FormData(loginForm).get("email") || "").trim(),
        password: String(new FormData(loginForm).get("password") || "")
      };

      try {
        const res = await fetch("/api/auth/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (!res.ok) {
          document.getElementById("loginMessage").textContent = data.message || "Login failed";
          return;
        }
        localStorage.setItem("eduConnectUser", JSON.stringify(data));
        redirectToDashboard(data.role);
      } catch (err) {
        document.getElementById("loginMessage").textContent = "Something went wrong.";
        console.error(err);
      }
    });
  }

  const forgotForm = document.getElementById("forgotPasswordForm");
  const verifyOtpForm = document.getElementById("verifyOtpForm");
  if (forgotForm && verifyOtpForm) {
    const emailInput = document.getElementById("forgotEmail");
    const message = document.getElementById("forgotMessage");

    forgotForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      const email = emailInput.value.trim();
      try {
        const res = await fetch("/api/auth/forgot-password/request-otp", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email })
        });
        const data = await res.json();
        message.textContent = data.message || "Unable to send OTP.";
        if (res.ok) {
          verifyOtpForm.classList.remove("hidden");
          emailInput.readOnly = true;
        }
      } catch (err) {
        message.textContent = "Something went wrong.";
        console.error(err);
      }
    });

    verifyOtpForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      const otp = String(new FormData(verifyOtpForm).get("otp") || "").trim();
      try {
        const res = await fetch("/api/auth/forgot-password/verify-otp", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: emailInput.value.trim(), otp })
        });
        const data = await res.json();
        if (!res.ok) {
          message.textContent = data.message || "Invalid or expired OTP.";
          return;
        }
        sessionStorage.setItem("eduConnectResetEmail", emailInput.value.trim());
        sessionStorage.setItem("eduConnectResetToken", data.resetToken);
        window.location.href = "reset-password.html";
      } catch (err) {
        message.textContent = "Something went wrong.";
        console.error(err);
      }
    });
  }

  const resetForm = document.getElementById("resetPasswordForm");
  if (resetForm) {
    const message = document.getElementById("resetMessage");
    resetForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      const formData = Object.fromEntries(new FormData(resetForm).entries());
      try {
        const res = await fetch("/api/auth/forgot-password/reset", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            email: sessionStorage.getItem("eduConnectResetEmail") || "",
            resetToken: sessionStorage.getItem("eduConnectResetToken") || "",
            newPassword: formData.newPassword,
            rewritePassword: formData.rewritePassword
          })
        });
        const data = await res.json();
        message.textContent = data.message || "Unable to reset password.";
        if (res.ok) {
          resetForm.classList.add("hidden");
          document.getElementById("resetLoginLink").classList.remove("hidden");
          sessionStorage.removeItem("eduConnectResetEmail");
          sessionStorage.removeItem("eduConnectResetToken");
        }
      } catch (err) {
        message.textContent = "Something went wrong.";
        console.error(err);
      }
    });
  }

}); // <-- closes the DOMContentLoaded listener from line 1

function normalizeRole(role) {
  if (!role) return "";
  const value = String(role).trim();
  if (value.toLowerCase() === "student") return "Student";
  if (value.toLowerCase() === "topper") return "Topper";
  if (value.toLowerCase() === "classowner" || value.toLowerCase() === "class owner") return "ClassOwner";
  return value;
}

function redirectToDashboard(role) {
  const normalized = normalizeRole(role);
  if (normalized === "Student") window.location.href = "student-dashboard.html";
  else if (normalized === "Topper") window.location.href = "topper-dashboard.html";
  else if (normalized === "ClassOwner") window.location.href = "owner-dashboard.html";
  else window.location.href = "index.html";
}