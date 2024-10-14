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

// Show the Add FAQ modal when Add FAQ button is clicked
document.getElementById('add-faq').addEventListener('click', function() {
    document.getElementById('add-faq-modal').style.display = 'block';
});

// Close the modal when Back button is clicked
document.getElementById('back-btn').addEventListener('click', function() {
    document.getElementById('add-faq-modal').style.display = 'none';
});

// Clear the input fields when Clear button is clicked
document.getElementById('clear-faq-btn').addEventListener('click', function() {
    document.getElementById('faq-question').value = '';
    document.getElementById('faq-answer').value = '';
});

// Submit the FAQ to the API when Add FAQ button is clicked
document.getElementById('submit-faq-btn').addEventListener('click', async function() {
    const question = document.getElementById('faq-question').value.trim();
    const answer = document.getElementById('faq-answer').value.trim();
    const errorMessageElement = document.getElementById('add-faq-error-message');

    // Clear any previous error messages
    errorMessageElement.style.display = 'none';
    errorMessageElement.innerText = '';

    // Input validation
    if (!question || !answer) {
        errorMessageElement.style.display = 'block'; // Show error box
        errorMessageElement.innerText = 'Please fill in both the question and the answer.';
        return;
    }

    const faqData = {
        Question: question,
        Answer: answer
    };

    try {
        const response = await fetch('http://localhost:5281/api/FAQ/add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + getJwtToken() // Add your JWT token here
            },
            body: JSON.stringify(faqData)
        });

        const responseData = await response.json();

        if (response.ok) {
            alert('FAQ added successfully!');
            document.getElementById('faq-question').value = '';
            document.getElementById('faq-answer').value = '';
            document.getElementById('add-faq-modal').style.display = 'none';
        } else {
            errorMessageElement.style.display = 'block'; // Show error box
            errorMessageElement.innerText = 'Error adding FAQ: ' + responseData.message;
        }
    } catch (error) {
        console.error('Error:', error);
        errorMessageElement.style.display = 'block'; // Show error box
        errorMessageElement.innerText = 'An error occurred while adding the FAQ.';
    }
});



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

            // Create buttons
            const buttonContainer = document.createElement('div');
            buttonContainer.className = 'button-container';

            // Create Preview button
            const previewButton = document.createElement('button');
            previewButton.className = 'preview-button';
            previewButton.innerText = 'Preview';
            previewButton.onclick = () => previewDocument(doc.file_Url); // Ensure preview works

            // Create Download button using an anchor
            const downloadLink = document.createElement('a');
            downloadLink.className = 'download-button';
            downloadLink.innerText = 'Download';
            downloadLink.href = doc.file_Url; // The URL to download the document
            downloadLink.download = `${doc.title}.${doc.file_Type}`; // Optional: specify a filename

            // Create Report button
            const reportButton = document.createElement('button');
            reportButton.className = 'report-button'; // Use your CSS class for report button
            reportButton.innerHTML = '<strong>!</strong>'; // Exclamation mark as content
            reportButton.onclick = () => openReportModal(doc.id); // Pass the document ID to the report modal

            // Append buttons to the container
            buttonContainer.appendChild(previewButton);
            buttonContainer.appendChild(downloadLink); // Append anchor as a button
            buttonContainer.appendChild(reportButton); // Append report button
            docElement.appendChild(buttonContainer);

            resultsContainer.appendChild(docElement);
        });

    } catch (error) {
        console.error('Error fetching search results:', error);
        searchErrorMessage.innerText = 'Error fetching search results. Please try again later.';
        searchErrorMessage.style.display = 'block'; // Show the error message
    }
}

function previewDocument(url) {
    const modal = document.getElementById('preview-modal');
    const iframe = document.getElementById('preview-iframe');
    
    // Set the iframe source to the document URL
    iframe.src = url;
    
    // Show the modal
    modal.style.display = 'block';
}

function closeModal() {
    const modal = document.getElementById('preview-modal');
    const iframe = document.getElementById('preview-iframe');
    
    // Clear the iframe source
    iframe.src = '';
    
    // Hide the modal
    modal.style.display = 'none';
}

// Opens the report modal and sets the document ID for reporting
function openReportModal(documentId) {
    const modal = document.getElementById('report-modal');
    document.getElementById('report-doc-id').value = documentId; // Set the document ID in a hidden input
    modal.style.display = 'block'; // Show the modal
}

// Closes the report modal
function closeReportModal() {
    const modal = document.getElementById('report-modal');
    modal.style.display = 'none'; // Hide the modal
}

// Handles report submission
async function submitReport() {
    const documentId = document.getElementById('report-doc-id').value.trim(); // Hidden field for document ID
    const reason = document.getElementById('report-reason').value.trim();

    // Input validation
    if (!documentId || !reason) {
        alert('Please fill in all fields.');
        return;
    }

    const reportData = {
        DocumentId: documentId,
        Reason: reason
    };

    try {
        // Submit the report data to the backend API
        const response = await fetch('http://localhost:5281/api/Reporting/CreateReport', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(reportData)
        });

        const responseData = await response.json();

        if (response.ok) {
            console.log('Report submitted successfully:', responseData);
            showFeedback("Report submitted successfully! ID: " + responseData.id);

            // Clear form fields after successful submission
            document.getElementById('report-reason').value = '';
        } else {
            console.error('Failed to submit report:', response.status, responseData);
            showFeedback("Error: " + (responseData.message || response.statusText));
        }
    } catch (error) {
        console.error('Error submitting report:', error);
        showFeedback('Error submitting report: ' + error.message);
    }

    // Close the report modal after submission
    closeReportModal();
}

// Function to display feedback messages
function showFeedback(message) {
    const feedbackElement = document.getElementById('create-response');
    if (feedbackElement) {
        feedbackElement.textContent = message;
    } else {
        console.warn('Feedback element not found. Message:', message);
        alert(message); // Fallback to alert if feedback element is missing
    }
}


function showFeedback(message) {
    const feedbackElement = document.getElementById('create-response');
    if (feedbackElement) {
        feedbackElement.textContent = message;
    } else {
        console.warn('Feedback element not found. Message:', message);
        alert(message);
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
async function performLogin(event) {
    event.preventDefault(); // Prevent the form from submitting and reloading the page
    
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
            console.log('Login successful:', result.message);
            console.log('Token:', result.token);
            console.log('User Name:', result.userName);

            // Store user information in localStorage for later use
            localStorage.setItem('userName', result.userName);
            localStorage.setItem('isLoggedIn', 'true'); // Mark user as logged in
            localStorage.setItem('userRole', result.role); // Storing the user role

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

            // Update UI elements after login
            loadUserProfile();

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


// Check login status on page load
document.addEventListener('DOMContentLoaded', () => {
    checkLoginStatus();
    loadUserProfile();
});

// Function to clear the login form
function clearLoginForm() {
    document.getElementById('email').value = '';
    document.getElementById('password').value = '';
    const errorMessage = document.getElementById('error-message');
    if (errorMessage) errorMessage.style.display = 'none'; // Hide the error message on clearing the form
}

// Helper function to validate email format using regex
function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}


// Function to load the user's profile icon and name after login
function loadUserProfile() {
    const userName = localStorage.getItem('userName');
    const profileIcon = document.getElementById('user-profile');
    const profileName = document.getElementById('profile-name');
    
    if (userName && profileIcon && profileName) {
        // Show the profile icon
        profileIcon.style.display = 'block'; 
        
        // Display initials or full name in the profile icon
        const initials = userName.split(' ').map(name => name.charAt(0).toUpperCase()).join('');
        profileName.textContent = initials; 
    } else {
        // Hide the profile icon if no user is logged in
        if (profileIcon) {
            profileIcon.style.display = 'none';
        }
    }
}

// Function to update the contribute tab and navigation
function updateContributeTabAndNavigation(userRole) {
    const contributeTab = document.getElementById('contribute');
    if (contributeTab) {
        // Remove or hide the introductory message paragraph (e.g., "Calling all educators")
        const introParagraph = contributeTab.querySelector('.contribute-message'); 
        if (introParagraph) {
            introParagraph.style.display = 'none'; // Hide the introductory paragraph
        }

        // Update heading and message for logged-in users
        contributeTab.querySelector('h2').innerText = 'Welcome Back!';
        const paragraph = contributeTab.querySelector('p');
        if (paragraph) {
            paragraph.innerText = 'We are excited to see what valuable resources you will share today!';
        }

        // Create and append the upload button
        const uploadBtn = document.createElement('button');
        uploadBtn.id = 'upload-btn';
        uploadBtn.className = 'upload-btn';
        uploadBtn.innerText = 'Upload';
        contributeTab.appendChild(uploadBtn);

        // Create and append the file input (hidden)
        const fileUpload = document.createElement('input');
        fileUpload.type = 'file';
        fileUpload.id = 'file-upload';
        fileUpload.style.display = 'none'; // Keep it hidden initially
        contributeTab.appendChild(fileUpload);

        // Add event listener to the upload button to trigger file selection
        uploadBtn.addEventListener('click', () => {
            fileUpload.click(); // Programmatically click the hidden file input
        });

        // Optional: Handle file selection
        fileUpload.addEventListener('change', (event) => {
            const selectedFile = event.target.files[0];
            if (selectedFile) {
                console.log('File selected:', selectedFile.name);
                // You can add additional code here to handle the file (e.g., upload it)
            }
        });
    }

    // Update dropdown menu: change the login button to logout
    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) logoutBtn.style.display = 'block'; // Show the logout button

    const loginLink = document.querySelector('a[href="#login"]');
    if (loginLink) loginLink.style.display = 'none'; // Hide the login link

    // Show FAQ management buttons only if the user is an admin
    if (userRole === 'admin') {
        const faqButtons = document.getElementById('faq-buttons');
        if (faqButtons) {
            faqButtons.style.display = 'flex'; // Make sure this line is executing
        }
    }
}



// Function to check login status and update UI on page load
function checkLoginStatus() {
    const isLoggedIn = localStorage.getItem('isLoggedIn');
    const userName = localStorage.getItem('userName');
    const userRole = 'admin' //localStorage.getItem('userRole'); // Get the stored user role

    if (isLoggedIn && userName) {
        // Update the user profile and UI elements
        loadUserProfile();
        updateContributeTabAndNavigation(userRole);
    }
}

// Function to handle logout
function performLogout() {
    localStorage.removeItem('userName');
    localStorage.removeItem('isLoggedIn');
    localStorage.removeItem('userRole');
    window.location.href = './index.html'; // Redirect to landing page
}


document.addEventListener('DOMContentLoaded', function () {
    const featureItems = document.querySelectorAll('.feature-item');
    
    featureItems.forEach((item, index) => {
      setTimeout(() => {
        item.classList.add('visible');
      }, index * 200); // Delay each feature's appearance by 200ms
    });
  });