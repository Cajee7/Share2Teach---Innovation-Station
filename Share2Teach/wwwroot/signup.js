document.addEventListener("DOMContentLoaded", function () {
  const signupForm = document.getElementById("signup-form");
  const passwordInput = document.getElementById("password");
  const confirmPasswordInput = document.getElementById("confirmPassword");
  const togglePassword = document.getElementById("togglePassword");
  const toggleConfirmPassword = document.getElementById("toggleConfirmPassword");
  const roleSelect = document.getElementById("role");
  const subjectsGroup = document.getElementById("subjects-group");
  const message = document.getElementById("message");
  
  // Toggle Password Visibility
  togglePassword.addEventListener("click", function () {
      const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
      passwordInput.setAttribute("type", type);
      togglePassword.textContent = type === "password" ? "👁️" : "👁️‍🗨️"; // Open/close eye icon
  });

  toggleConfirmPassword.addEventListener("click", function () {
      const type = confirmPasswordInput.getAttribute("type") === "password" ? "text" : "password";
      confirmPasswordInput.setAttribute("type", type);
      toggleConfirmPassword.textContent = type === "password" ? "👁️" : "👁️‍🗨️"; // Open/close eye icon
  });

  // Show subjects input only for "Teacher" role
  roleSelect.addEventListener("change", function () {
      if (roleSelect.value === "teacher") {
          subjectsGroup.style.display = "block";
      } else {
          subjectsGroup.style.display = "none";
      }
  });

  // Handle form submission
  signupForm.addEventListener("submit", async function (e) {
      e.preventDefault();

      const firstName = document.getElementById("firstName").value;
      const lastName = document.getElementById("lastName").value;
      const email = document.getElementById("email").value;
      const role = document.getElementById("role").value;
      const password = document.getElementById("password").value;
      const confirmPassword = document.getElementById("confirmPassword").value;
      const subjects = role === "teacher" ? document.getElementById("subjects").value.split(',').map(subject => subject.trim()) : [];

      if (password !== confirmPassword) {
          message.textContent = "Passwords do not match.";
          return;
      }

      // Prepare form data
      const formData = new FormData();
      formData.append("FirstName", firstName);
      formData.append("LastName", lastName);
      formData.append("Email", email);
      formData.append("Role", role);
      formData.append("Password", password);
      formData.append("ConfirmPassword", confirmPassword);

      if (role === "teacher") {
          formData.append("Subjects", subjects);
      }

      try {
          const response = await fetch('http://localhost:5281/api/Authenticate/register', {
              method: 'POST',
              body: formData
          });

          const result = await response.json();

          if (response.ok) {
              // Show success message
              message.textContent = "Account created successfully! Redirecting...";
              
              // Redirect to index.html after 2 seconds
              setTimeout(() => {
                  window.location.href = "index.html";
              }, 2000);
          } else {
              // Show error message from the response
              message.textContent = result.message || "Registration failed.";
          }
      } catch (error) {
          console.error('Error:', error);
          message.textContent = "An error occurred during registration.";
      }
  });

  // Clear form fields
  document.getElementById("clearButton").addEventListener("click", function () {
      signupForm.reset();
      subjectsGroup.style.display = "none"; // Hide subjects if it was shown
      message.textContent = "";
  });
});
