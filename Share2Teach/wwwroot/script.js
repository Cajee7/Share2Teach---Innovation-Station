// Function to open tabs and handle dynamic loading
function openTab(evt, tabName) {
    const tabContents = document.getElementsByClassName("tab-content");
    for (let i = 0; i < tabContents.length; i++) {
        tabContents[i].style.display = "none"; // Hide all tab content
    }

    const tabButtons = document.getElementsByClassName("tab-button");
    for (let i = 0; i < tabButtons.length; i++) {
        tabButtons[i].className = tabButtons[i].className.replace(" active", ""); // Remove active class
    }

    document.getElementById(tabName).style.display = "block"; // Show the selected tab content
    evt.currentTarget.className += " active"; // Set the active class

    // Load FAQs if the FAQ tab is opened
    if (tabName === 'faq') {
        loadFAQs(); // Load FAQs when FAQ tab is opened
    }
}

// Function to fetch FAQs from the API
async function loadFAQs() {
    const faqErrorMessage = document.getElementById('faq-error-message'); // Error message for FAQs
    faqErrorMessage.style.display = 'none'; // Hide previous error messages

    try {
        const response = await fetch(`http://localhost:5281/api/FAQ/list`); // Replace with your actual API endpoint
        if (!response.ok) throw new Error('Network response was not ok');

        const faqs = await response.json();
        renderFAQs(faqs); // Render the fetched FAQs
    } catch (error) {
        faqErrorMessage.innerText = 'Error fetching FAQs. Please try again later.';
        faqErrorMessage.style.display = 'block'; // Show the error message
    }
}

// Function to render FAQs
function renderFAQs(faqs) {
    const faqContainer = document.getElementById('faq-list-container'); // ID from your HTML for the FAQ list
    faqContainer.innerHTML = ''; // Clear existing FAQs

    if (faqs.length === 0) {
        faqContainer.innerHTML = '<p>No FAQs available.</p>';
        return;
    }

    faqs.forEach(faq => {
        const faqItem = document.createElement('div');
        faqItem.className = 'faq-item'; // Add a class for styling
        faqItem.innerHTML = `
            <h3 class="question">Q: ${faq.question || 'No question available'}</h3>
            <p class="answer">A: ${faq.answer || 'No answer available'}</p>
            <small>Date uploaded: ${faq.dateAdded ? new Date(faq.dateAdded).toLocaleDateString() : 'Date not available'}</small>
        `;
        faqContainer.appendChild(faqItem);
    });
}

// Function to perform search
async function performSearch() {
    const searchInputElement = document.getElementById('search-input');
    const resultsContainer = document.getElementById('results-container');
    const searchErrorMessage = document.getElementById('search-error-message'); // Error message for search
    searchErrorMessage.style.display = 'none'; // Hide previous error messages

    // Clear previous results
    resultsContainer.innerHTML = '';

    // Check if the search input element exists
    if (!searchInputElement) {
        searchErrorMessage.innerText = 'Search input not found. Please check the HTML structure.';
        searchErrorMessage.style.display = 'block';
        return;
    }

    const query = searchInputElement.value.trim();
    
    if (!query) {
        searchErrorMessage.innerText = 'Please enter a search term.';
        searchErrorMessage.style.display = 'block';
        return;
    }

    try {
        const encodedQuery = encodeURIComponent(query);
        const response = await fetch(`http://localhost:5281/api/File/Search?query=${encodedQuery}`);
        if (!response.ok) throw new Error('Network response was not ok');

        const results = await response.json();

        if (results.length === 0) {
            resultsContainer.innerHTML = '<p>No documents found matching the search criteria.</p>';
            return;
        }

        // Display the results
        results.forEach(doc => {
            const docElement = document.createElement('div');
            docElement.className = 'doc-item';
        
            const tags = doc.tags && doc.tags.length > 0 ? doc.tags.join(', ') : 'No tags available';
        
            docElement.innerHTML = `
                <p><strong>Title:</strong> ${doc.title || 'No title available'}</p>
                <p><strong>Subject:</strong> ${doc.subject || 'No subject available'}</p>
                <p><strong>Grade:</strong> ${doc.grade || 'No grade available'}</p>
                <p><strong>Description:</strong> ${doc.description || 'No description available'}</p>
                <p><strong>File Size:</strong> ${doc.file_Size ? doc.file_Size + ' MB' : 'File size not available'}</p>
                <p><strong>Tags:</strong> ${tags}</p>
                <p><strong>Date Uploaded:</strong> ${doc.date_Uploaded ? new Date(doc.date_Uploaded).toLocaleDateString() : 'Date not available'}</p>
                <p><strong>Date Updated:</strong> ${doc.date_Updated ? new Date(doc.date_Updated).toLocaleDateString() : 'Date not available'}</p>
            `;
            resultsContainer.appendChild(docElement);
        });
        
    } catch (error) {
        console.error('Error fetching search results:', error); 
        searchErrorMessage.innerText = 'Error fetching search results. Please try again later.';
        searchErrorMessage.style.display = 'block'; // Show the error message
    }
}

// Function to show error messages for login
function showError(message, errorType) {
    let errorMessage; 
    if (errorType === 'login') {
        errorMessage = document.getElementById('error-message'); // Assuming you have a separate div for login errors
    } else {
        errorMessage = document.getElementById('error-message'); // Change this logic as needed
    }
    
    errorMessage.innerText = message;
    errorMessage.style.display = 'block';

    // Automatically hide the error message after 5 seconds
    setTimeout(() => {
        errorMessage.style.display = 'none';
    }, 5000);
}

// Function to perform login
async function performLogin() {
    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value.trim();
    const errorMessage = document.getElementById('error-message');
    const successMessage = document.getElementById('success-message'); // Success message element
    errorMessage.style.display = 'none'; // Hide the error message by default
    successMessage.style.display = 'none'; // Hide the success message by default

    let errors = [];

    // Validate if fields are not empty
    if (!email) {
        errors.push('Email is required.');
    } else if (!validateEmail(email)) {
        errors.push('Please enter a valid email address.');
    }

    if (!password) {
        errors.push('Password is required.');
    } else if (password.length < 6) {
        errors.push('Password must be at least 6 characters long.');
    }

    // Display error messages if there are any
    if (errors.length > 0) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = errors.join('<br>'); // Join errors with a line break for multiple errors
        return;
    }

    // Prepare FormData to send to the server
    const formData = new FormData();
    formData.append('Email', email);
    formData.append('Password', password);

    try {
        // Perform login logic by sending data to the backend
        const response = await fetch('http://localhost:5281/api/Authenticate/login', {
            method: 'POST',
            body: formData
        });

        // Check if the login was successful
        if (response.ok) {
            const result = await response.json();
            console.log('Login successful:', result.message); // Display success message
            console.log('Token:', result.token); // You can store the token or handle it accordingly
            console.log('User Name:', result.userName); // Assuming you get the user's name in the response

            // Store user information in localStorage for later use
            localStorage.setItem('userName', result.userName);
            localStorage.setItem('isLoggedIn', 'true'); // Mark user as logged in

            // Show animated success popup
            successMessage.style.display = 'block';
            successMessage.innerHTML = 'Logged in successfully!';
            successMessage.style.backgroundColor = "#4CAF50"; // Green for success
            successMessage.style.bottom = '-100px'; // Initially below the page
            successMessage.style.opacity = '0';    // Initially transparent

            setTimeout(() => {
                successMessage.style.bottom = '20px'; // Slide it up
                successMessage.style.opacity = '1';   // Fade in
            }, 100);

            // Hide the popup after 2 seconds
            setTimeout(() => {
                successMessage.style.bottom = '-100px'; // Slide back down
                successMessage.style.opacity = '0';     // Fade out
            }, 2000);

            // Completely hide the message after the animation ends
            setTimeout(() => {
                successMessage.style.display = 'none';
                // Redirect to the landing page (index.html) after successful login
                window.location.href = './index.html';  // Ensure the path is correct
            }, 2300);

            // Update UI elements
            updateUIAfterLogin();

        } else if (response.status === 400) {
            // Handle validation errors from the backend
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = 'Invalid input. Please check your email and password.';
        } else if (response.status === 401) {
            // Handle unauthorized login attempt
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = 'Invalid login attempt. Please check your email and password.';
        } else {
            // Handle other server-side errors
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = 'An error occurred while logging in. Please try again later.';
        }
    } catch (error) {
        // Handle network or unexpected errors
        console.error('An error occurred:', error);
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'An unexpected error occurred. Please try again.';
    }
}

// Function to check login status and update UI on page load
function checkLoginStatus() {
    const isLoggedIn = localStorage.getItem('isLoggedIn');
    const userName = localStorage.getItem('userName');

    if (isLoggedIn) {
        document.getElementById('user-profile').style.display = 'block';
        document.getElementById('profile-name').innerText = userName;

        // Change the contribute tab content
        const contributeTab = document.getElementById('contribute');
        contributeTab.querySelector('h2').innerText = 'Welcome Back!';
        contributeTab.querySelector('p').innerText = 'Feel free to share your valuable resources!';

        // Update dropdown menu: change the login button to logout
        const logoutBtn = document.getElementById('logout-btn');
        logoutBtn.style.display = 'block'; // Show the logout button
        const loginLink = document.querySelector('a[href="#login"]');
        if (loginLink) {
            loginLink.style.display = 'none'; // Hide the login link
        }
    }
}

// Function to update UI elements after login
function updateUIAfterLogin() {
    const userName = localStorage.getItem('userName');
    const contributeTab = document.getElementById('contribute');
    
    contributeTab.querySelector('h2').innerText = 'Welcome Back!';
    contributeTab.querySelector('p').innerText = 'Feel free to share your valuable resources!';

    // Update dropdown menu: change the login button to logout
    const logoutBtn = document.getElementById('logout-btn');
    logoutBtn.style.display = 'block'; // Show the logout button
    const loginLink = document.querySelector('a[href="#login"]');
    if (loginLink) {
        loginLink.style.display = 'none'; // Hide the login link
    }
}

// Function to handle logout
function performLogout() {
    localStorage.removeItem('userName');
    localStorage.removeItem('isLoggedIn');
    window.location.href = './index.html'; // Redirect to landing page
}

// Check login status on page load
document.addEventListener('DOMContentLoaded', checkLoginStatus);

// Function to clear the login form
function clearLoginForm() {
    document.getElementById('email').value = '';
    document.getElementById('password').value = '';
    const errorMessage = document.getElementById('error-message');
    errorMessage.style.display = 'none'; // Hide the error message on clearing the form
}

// Helper function to validate email format using regex
function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// Function to load the user's profile icon and name after login
window.onload = function() {
    const userName = localStorage.getItem('userName');
    
    if (userName) {
        // Display the user's profile icon with their initials or name
        const profileIcon = document.getElementById('user-profile');
        const profileName = document.getElementById('profile-name');

        profileIcon.style.display = 'block'; // Show the profile icon
        profileName.textContent = userName.charAt(0).toUpperCase(); // Display the first letter of the user's name as an avatar
    }
};
