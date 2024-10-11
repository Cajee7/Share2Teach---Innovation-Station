document.getElementById('signup-form').addEventListener('submit', async function(event) {
    event.preventDefault(); // Prevent the default form submission

    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const subjects = document.getElementById('subjects').value.split(',').map(subject => subject.trim()).filter(subject => subject);

    // Clear previous error messages
    clearErrorMessages();

    // Validations
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/; // Basic email validation
    const errorMessages = [];

    if (!firstName) {
        errorMessages.push("First name cannot be empty.");
    }
    
    if (!lastName) {
        errorMessages.push("Last name cannot be empty.");
    }

    if (!email) {
        errorMessages.push("Email cannot be empty.");
    } else if (!emailPattern.test(email)) {
        errorMessages.push("Please enter a valid email address.");
    }

    if (!password) {
        errorMessages.push("Password cannot be empty.");
    } else if (password.length < 8) {
        errorMessages.push("Password must be at least 8 characters long.");
    }

    if (!confirmPassword) {
        errorMessages.push("Confirm password cannot be empty.");
    } else if (password !== confirmPassword) {
        errorMessages.push("Passwords do not match.");
    }

    if (subjects.length === 0) {
        errorMessages.push("Please provide at least one subject.");
    }

    if (errorMessages.length > 0) {
        showErrorMessages(errorMessages);
        return;
    }

    // Continue with the form submission logic (AJAX call or form submission)
    const response = await fetch('api/Authenticate/register', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Password: password,
            ConfirmPassword: confirmPassword,
            Role: 'teacher', // Automatically assign role as teacher
            Subjects: subjects
        })
    });

    const data = await response.json();

    if (response.ok) {
        showMessage(data.message);
    } else {
        showErrorMessages([data.message || "An error occurred."]);
    }
});

function showErrorMessages(messages) {
    const errorBox = document.getElementById('error-box');
    const errorList = document.getElementById('error-list');

    // Clear previous error messages
    errorList.innerHTML = '';

    // Add new error messages to the list
    messages.forEach(message => {
        const li = document.createElement('li');
        li.textContent = message;
        errorList.appendChild(li);
    });

    // Show the error box
    errorBox.style.display = 'block';
}

function clearErrorMessages() {
    const errorBox = document.getElementById('error-box');
    errorBox.style.display = 'none'; // Hide the error box
    const errorList = document.getElementById('error-list');
    errorList.innerHTML = ''; // Clear the error list
}
