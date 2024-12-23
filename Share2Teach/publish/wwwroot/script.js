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
        const response = await fetch('http://localhost:5281/api/FAQ/list'); // Replace with your actual API endpoint
        if (!response.ok) throw new Error('Network response was not ok');

        const faqs = await response.json();
        renderFAQs(faqs); // Render the fetched FAQs
    } catch (error) {
        faqErrorMessage.innerText = 'Error fetching FAQs. Please try again later.';
        faqErrorMessage.style.display = 'block'; // Show the error message
    }
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
            localStorage.setItem('token', result.token); //Strong jwt toke

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
            }, 20000);

            // Completely hide the message after the animation ends
            setTimeout(() => {
                successMessage.style.display = 'none';
                // Redirect to the landing page (index.html) after successful login
                window.location.href = './index.html';  // Ensure the path is correct
            }, 23000);

            // Update UI elements after login
            loadUserProfile();
            updateContributeTabAndNavigation(result.role);

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

function getAuthToken() {
    const token = localStorage.getItem('token');
    if (!token) {
        console.warn('No authentication token found. User might need to log in.');
        return null;
    }
    return token;
}

function checkLoginStatus() {
    const isLoggedIn = localStorage.getItem('isLoggedIn');
    const userName = localStorage.getItem('userName');
    const userRole = localStorage.getItem('userRole');

    if (isLoggedIn && userName) {
        loadUserProfile();
        updateContributeTabAndNavigation(userRole);
    }
}

// Call this function when the page loads
document.addEventListener('DOMContentLoaded', checkLoginStatus);

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

document.querySelector('.forgot-password a').addEventListener('click', function() {
    document.getElementById('login').style.display = 'none'; // Hide login form
    document.getElementById('forgot-password').style.display = 'block'; // Show forgot password form
});


async function submitForgotPassword(event) {
    event.preventDefault();
    const email = document.getElementById('forgot-email').value.trim();
    const errorMessage = document.getElementById('forgot-error-message');
    const successMessage = document.getElementById('forgot-success-message');

    errorMessage.style.display = 'none';
    successMessage.style.display = 'none';

    // Validate email
    if (!email) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'Email is required.';
        return;
    }

    // Prepare FormData
    const formData = new FormData();
    formData.append('Email', email);

    try {
        const response = await fetch('http://localhost:5281/api/Authenticate/forgot-password', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            successMessage.style.display = 'block';
            successMessage.innerHTML = 'Password reset token sent to your email.';
            // Optionally show reset password form
            document.getElementById('forgot-password').style.display = 'none';
            document.getElementById('reset-password').style.display = 'block';
        } else {
            const result = await response.json();
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = result.message || 'An error occurred.';
        }
    } catch (error) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'An unexpected error occurred. Please try again.';
    }
}

async function submitResetPassword(event) {
    event.preventDefault();
    const token = document.getElementById('reset-token').value.trim();
    const newPassword = document.getElementById('new-password').value.trim();
    const confirmPassword = document.getElementById('confirm-password').value.trim();
    const errorMessage = document.getElementById('reset-error-message');
    const successMessage = document.getElementById('reset-success-message');

    errorMessage.style.display = 'none';
    successMessage.style.display = 'none';

    // Validate passwords
    if (newPassword !== confirmPassword) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'Passwords do not match.';
        return;
    }
    
    // Prepare FormData
    const formData = new FormData();
    formData.append('Token', token);
    formData.append('NewPassword', newPassword);
    formData.append('ConfirmPassword', confirmPassword);

    try {
        const response = await fetch('http://localhost:5281/api/Authenticate/reset-password', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            successMessage.style.display = 'block';
            successMessage.innerHTML = 'Password has been reset successfully. You can now log in with your new password.';
            // Redirect to login or show login form
            // Optionally: window.location.href = './login.html'; // Uncomment to redirect
            

            
        } else {
            const result = await response.json();
            errorMessage.style.display = 'block';
            errorMessage.innerHTML = result.message || 'An error occurred.';
        }
    } catch (error) {
        errorMessage.style.display = 'block';
        errorMessage.innerHTML = 'An unexpected error occurred. Please try again.';
    }
}

function clearForgotPasswordForm() {
    document.getElementById('forgot-password-form').reset();
    document.getElementById('forgot-error-message').style.display = 'none';
    document.getElementById('forgot-success-message').style.display = 'none';
}

function clearResetPasswordForm() {
    document.getElementById('reset-password-form').reset();
    document.getElementById('reset-error-message').style.display = 'none';
    document.getElementById('reset-success-message').style.display = 'none';
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
        faqItem.className = 'faq-item'; 
        faqItem.innerHTML = `
            <h3 class="question">Q: ${faq.question || 'No question available'}</h3>
            <p class="answer">A: ${faq.answer || 'No answer available'}</p>
            <small>Date uploaded: ${faq.dateAdded ? new Date(faq.dateAdded).toLocaleDateString() : 'Date not available'}</small>
            <h4 class="faq-id"><span>ID: ${faq.id || 'N/A'}</span></h3>  <!-- Updated to use id -->
        `;
        faqContainer.appendChild(faqItem);
    });
}



// Global variable to track current operation
let currentOperation = '';

// Define apiUrl (you may want to set this to your actual API URL)
const apiUrl = 'http://localhost:5281/api/FAQ';

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM fully loaded and parsed');

    const faqModal = document.getElementById('faq-modal');
    const addFaqButton = document.getElementById('add-faq');
    const updateFaqButton = document.getElementById('update-faq');
    const deleteFaqButton = document.getElementById('delete-faq');
    const backBtn = document.getElementById('back-btn');
    const submitBtn = document.getElementById('submit-btn');
    const clearBtn = document.getElementById('clear-btn');

    console.log('Back button element:', backBtn);

    // Open modal for Add operation
    if (addFaqButton) {
        addFaqButton.addEventListener('click', () => openModal('add'));
    }

    // Open modal for Update operation
    if (updateFaqButton) {
        updateFaqButton.addEventListener('click', () => openModal('update'));
    }

    // Open modal for Delete operation
    if (deleteFaqButton) {
        deleteFaqButton.addEventListener('click', () => openModal('delete'));
    }

    // Submit form
    if (submitBtn) {
        submitBtn.addEventListener('click', handleSubmit);
    }

    // Clear form
    if (clearBtn) {
        clearBtn.addEventListener('click', clearForm);
    }
});

function openModal(operation) {
    console.log('Opening modal for operation:', operation);
    currentOperation = operation;
    const modal = document.getElementById('faq-modal');
    const modalTitle = document.getElementById('modal-title');
    const faqIdField = document.getElementById('faq-id-field');
    const questionField = document.getElementById('question-field');
    const answerField = document.getElementById('answer-field');

    if (modal) {
        modal.style.display = 'block'; // Make modal visible
    } else {
        console.error('Modal element not found when opening modal');
        return;
    }

    switch (operation) {
        case 'add':
            modalTitle.textContent = 'Add New FAQ';
            faqIdField.style.display = 'none';
            questionField.style.display = 'block';
            answerField.style.display = 'block';
            clearForm();
            break;
        case 'update':
            modalTitle.textContent = 'Update FAQ';
            faqIdField.style.display = 'block';
            questionField.style.display = 'block';
            answerField.style.display = 'block';
            break;
        case 'delete':
            modalTitle.textContent = 'Delete FAQ';
            faqIdField.style.display = 'block';
            questionField.style.display = 'none';
            answerField.style.display = 'none';
            clearForm();
            break;
    }
}

// Close FAQ Modal
function closeFaqModal() {
    const faqModal = document.getElementById('faq-modal');
    faqModal.style.display = "none";
  }

function clearForm() {
    document.getElementById('faq-id').value = '';
    document.getElementById('question').value = '';
    document.getElementById('answer').value = '';
}

async function handleSubmit() {
    const faqId = document.getElementById('faq-id').value;
    const question = document.getElementById('question').value;
    const answer = document.getElementById('answer').value;

    switch (currentOperation) {
        case 'add':
            await addFaq(question, answer);
            break;
        case 'update':
            await updateFaq(faqId, question, answer);
            break;
        case 'delete':
            await deleteFaq(faqId);
            break;
    }
}

async function addFaq(question, answer) {
    if (!question || !answer) {
        showPopup("Please fill in all fields", "error");
        return;
    }

    const authToken = getAuthToken();
    if (!authToken) {
        showPopup('Authentication token not found. Please log in.', "error");
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/add`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ question, answer })
        });

        if (response.ok) {
            showPopup("FAQ added successfully", "success");
            closeFaqModal();
            loadFAQs();
        } else {
            showPopup("Error adding FAQ", "error");
        }
    } catch (error) {
        console.error('Error:', error);
        showPopup("An error occurred while adding the FAQ", "error");
    }
}

async function updateFaq(faqId, question, answer) {
    if (!faqId || !question || !answer) {
        showPopup("Please fill in all fields", "error");
        return;
    }

    const authToken = getAuthToken();
    if (!authToken) {
        showPopup('Authentication token not found. Please log in.', "error");
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/update?id=${faqId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ question, answer })
        });

        if (response.ok) {
            showPopup("FAQ updated successfully", "success");
            closeFaqModal();
            loadFAQs();
        } else if (response.status === 404) {
            showPopup("FAQ not found", "error");
        } else {
            showPopup("Error updating FAQ", "error");
        }
    } catch (error) {
        console.error('Error:', error);
        showPopup("An error occurred while updating the FAQ", "error");
    }
}

async function deleteFaq(faqId) {
    if (!faqId) {
        showPopup("Please enter an FAQ ID to delete.", "error");
        return;
    }

    const authToken = getAuthToken();
    if (!authToken) {
        showPopup('Authentication token not found. Please log in.', "error");
        return;
    }

    try {
        const response = await fetch(`${apiUrl}/delete?id=${faqId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });
        if (response.ok) {
            showPopup("FAQ deleted successfully", "success");
            closeFaqModal();
            loadFAQs();
        } else if (response.status === 404) {
            showPopup("FAQ not found", "error");
        } else {
            showPopup("Error deleting FAQ", "error");
        }
    } catch (error) {
        console.error('Error:', error);
        showPopup("An error occurred while deleting the FAQ", "error");
    }
}

function showPopup(message, type) {
    const popup = document.createElement('div');
    popup.textContent = message;
    popup.style.position = 'fixed';
    popup.style.top = '20px';
    popup.style.left = '50%';
    popup.style.transform = 'translateX(-50%)';
    popup.style.padding = '10px 20px';
    popup.style.borderRadius = '5px';
    popup.style.color = 'white';
    popup.style.zIndex = '1000';

    if (type === 'error') {
        popup.style.backgroundColor = 'red';
    } else if (type === 'success') {
        popup.style.backgroundColor = 'green';
    }

    document.body.appendChild(popup);

    setTimeout(() => {
        document.body.removeChild(popup);
    }, 3000);
}


async function performSearch() {
    console.log('Performing search...');
    const searchInputElement = document.getElementById('search-input');
    const resultsContainer = document.getElementById('results-container');
    const searchErrorMessage = document.getElementById('search-error-message');
    searchErrorMessage.style.display = 'none';

    resultsContainer.innerHTML = '';

    if (!searchInputElement) {
        console.error('Search input element not found');
        searchErrorMessage.innerText = 'Search input not found. Please check the HTML structure.';
        searchErrorMessage.style.display = 'block';
        return;
    }

    const query = searchInputElement.value.trim();
    
    if (!query) {
        console.log('Empty search query');
        searchErrorMessage.innerText = 'Please enter a search term.';
        searchErrorMessage.style.display = 'block';
        return;
    }

    try {
        console.log('Fetching search results for query:', query);
        const encodedQuery = encodeURIComponent(query);
        const response = await fetch(`http://localhost:5281/api/File/Search?query=${encodedQuery}`);
        if (!response.ok) throw new Error('Network response was not ok');

        const results = await response.json();
        console.log('Search results:', results);

        if (results.length === 0) {
            console.log('No results found');
            resultsContainer.innerHTML = '<p>No documents found matching the search criteria.</p>';
            return;
        }

        results.forEach((doc, index) => {
            console.log(`Processing document ${index + 1}:`, doc);
            const docElement = document.createElement('div');
            docElement.className = 'doc-item';
        
            const tags = doc.tags?.join(', ') ?? 'No tags available';
        
            docElement.innerHTML = `
                <p><strong>Title:</strong> ${doc.title ?? 'No title available'}</p>
                <p><strong>Subject:</strong> ${doc.subject ?? 'No subject available'}</p>
                <p><strong>Grade:</strong> ${doc.grade ?? 'No grade available'}</p>
                <p><strong>Description:</strong> ${doc.description ?? 'No description available'}</p>
                <p><strong>File Size:</strong> ${doc.file_Size ? `${doc.file_Size} MB` : 'File size not available'}</p>
                <p><strong>Tags:</strong> ${tags}</p>
                <p><strong>Date Uploaded:</strong> ${doc.date_Uploaded ? new Date(doc.date_Uploaded).toLocaleDateString() : 'Date not available'}</p>
                <p><strong>Date Updated:</strong> ${doc.date_Updated ? new Date(doc.date_Updated).toLocaleDateString() : 'Date not available'}</p>
            `;


            const buttonContainer = document.createElement('div');
            buttonContainer.className = 'button-container';

            // Preview button
            const previewButton = document.createElement('button');
            previewButton.className = 'preview-button';
            previewButton.innerText = 'Preview';
            if (doc.file_Url) {
                previewButton.onclick = function() {
                    console.log('Preview button clicked for document:', doc.title);
                    previewDocument(doc.file_Url);
                };
            } else {
                previewButton.disabled = true;
                previewButton.title = 'Preview not available';
                console.log('Preview not available for document:', doc.title);
            }

            // Download button
            const downloadButton = document.createElement('button');
            downloadButton.className = 'download-button';
            downloadButton.innerText = 'Download';
            if (doc.file_Url) {
                downloadButton.onclick = function() {
                    console.log('Download button clicked for document:', doc.title);
                    downloadDocument(doc.file_Url, doc.title);
                };
            } else {
                downloadButton.disabled = true;
                downloadButton.title = 'Download not available';
                console.log('Download not available for document:', doc.title);
            }

            // Report button
            const reportButton = document.createElement('button');
            reportButton.className = 'report-button';
            reportButton.innerHTML = '<strong>!</strong>';
            reportButton.onclick = function() {
                console.log('Report button clicked for document:', doc.title);
                if (doc.id !== undefined) {
                    openReportModal(doc.id);
                } else {
                    console.error('Document ID is undefined for:', doc.title);
                    alert('Unable to report this document. Document ID is missing.');
                }
            };

            buttonContainer.appendChild(previewButton);
            buttonContainer.appendChild(downloadButton);
            buttonContainer.appendChild(reportButton);
            docElement.appendChild(buttonContainer);

            resultsContainer.appendChild(docElement);
        });

    } catch (error) {
        console.error('Error fetching search results:', error);
        searchErrorMessage.innerText = 'Error fetching search results. Please try again later.';
        searchErrorMessage.style.display = 'block';
    }
}

function previewDocument(url) {
    console.log('Previewing document:', url);
    const modal = document.getElementById('preview-modal');
    const iframe = document.getElementById('preview-iframe');
    
    if (!modal || !iframe) {
        console.error('Modal or iframe element not found');
        alert('Preview functionality is not available. Please check your HTML structure.');
        return;
    }
    
    if (!url) {
        console.error('Invalid URL for preview:', url);
        alert('Preview is not available for this document.');
        return;
    }
    
    iframe.src = url;
    modal.style.display = 'block';
}

function downloadDocument(url, title) {
    console.log('Downloading document:', url, 'with title:', title);
    if (!url) {
        console.error('Invalid URL for download:', url);
        alert('Download is not available for this document.');
        return;
    }

    const link = document.createElement('a');
    link.href = url;
    link.download = title || 'document';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Close Preview Modal
function closePreviewModal() {
    const previewModal = document.getElementById('preview-modal');
    previewModal.style.display = "none";
}

// Opens the report modal and sets the document ID for reporting
function openReportModal(documentId) {
    console.log('openReportModal called with documentId:', documentId);
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


function updateContributeTabAndNavigation(userRole) {
    const contributeTab = document.getElementById('contribute');
    if (contributeTab) {
        // Remove or hide the introductory message paragraph
        const introParagraph = contributeTab.querySelector('.contribute-message'); 
        if (introParagraph) {
            introParagraph.style.display = 'none';
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
        fileUpload.style.display = 'none';
        contributeTab.appendChild(fileUpload);

        // Create and append the upload form (initially hidden)
        const uploadForm = document.createElement('div');
        uploadForm.id = 'upload-form';
        uploadForm.style.display = 'none';
        uploadForm.innerHTML = `
            <input type="text" id="title" placeholder="Title" />
            <input type="text" id="subject" placeholder="Subject" />
            <input type="text" id="grade" placeholder="Grade (integer)" /> 
            <textarea id="description" placeholder="Description"></textarea>
            <button id="clear-btn" style="background-color: red; color: white;">Clear</button> 
        `;
        contributeTab.appendChild(uploadForm);

        // Maximum file size in MB
        const maxFileSizeMb = 10;

        // Allowed file types
        const allowedFileTypes = ['.pdf', '.doc', '.docx'];

        // Add event listener to the upload button
        uploadBtn.addEventListener('click', () => {
            if (uploadForm.style.display === 'block') {
                handleUpload();
            } else {
                fileUpload.click();
            }
        });

        // Handle file selection
        fileUpload.addEventListener('change', (event) => {
            const file = event.target.files[0];
            if (file) {
                try {
                    // Validate file size
                    if (file.size > maxFileSizeMb * 1024 * 1024) {
                        throw new Error(`File size exceeds the limit of ${maxFileSizeMb} MB.`);
                    }
                
                    // Validate file type
                    const fileType = '.' + file.name.split('.').pop().toLowerCase();
                    if (!allowedFileTypes.includes(fileType)) {
                        throw new Error(`File type '${fileType}' is not allowed. Allowed types are: ${allowedFileTypes.join(', ')}`);
                    }

                    // Show the upload form
                    uploadForm.style.display = 'block';
                } catch (error) {
                    console.error('Upload error:', error.message);
                    showMessage(error.message, 'error');
                }
            }
        });

        // Function to handle the upload process
        async function handleUpload() {
            // Get input elements
            const titleInput = document.getElementById('title');
            const subjectInput = document.getElementById('subject');
            const gradeInput = document.getElementById('grade');
            const descriptionInput = document.getElementById('description');
        
            // Check if elements exist
            if (!titleInput || !subjectInput || !gradeInput || !descriptionInput) {
                return showMessage('One or more form elements are missing. Please check the HTML structure.', 'error');
            }
        
            // Validate inputs
            if (!titleInput.value.trim() || !subjectInput.value.trim() || !gradeInput.value.trim() || !descriptionInput.value.trim()) {
                return showMessage('Please fill in all fields.', 'error');
            }

            // Validate grade is an integer
            const gradeValue = parseInt(gradeInput.value, 10);
            if (isNaN(gradeValue) || gradeValue <= 0) {
                return showMessage('Please enter a valid grade as an integer.', 'error');
            }

            // Prepare form data
            const formData = new FormData();
            formData.append('UploadedFile', fileUpload.files[0]);
            formData.append('Title', titleInput.value);
            formData.append('Subject', subjectInput.value);
            formData.append('Grade', gradeValue);
            formData.append('Description', descriptionInput.value);

            // Get the authentication token
            const authToken = getAuthToken();
            if (!authToken) {
                return showMessage('Authentication token not found. Please log in.', 'error');
            }

            try {
                // Send file and metadata to server
                const response = await fetch('http://localhost:5281/api/File/upload', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    }
                });
            
                if (!response.ok) {
                    throw new Error(`Upload failed: ${response.statusText}`);
                }

                const result = await response.json();
                console.log('Upload successful:', result.message);
                console.log('Generated tags:', result.tags);

                // Clear the file input and reset form
                fileUpload.value = '';
                titleInput.value = '';
                subjectInput.value = '';
                gradeInput.value = '';
                descriptionInput.value = '';

                // Hide the form again
                uploadForm.style.display = 'none';

                // Update UI to show success message
                showPopup("Success!! File uploaded successfully!");

            } catch (error) {
                console.error('Upload error:', error.message);
                showMessage(error.message, 'error');
            }
        }

        // Add event listener for the clear button
        const clearBtn = document.getElementById('clear-btn');
        clearBtn.addEventListener('click', () => {
            document.getElementById('title').value = '';
            document.getElementById('subject').value = '';
            document.getElementById('grade').value = '';
            document.getElementById('description').value = '';
            fileUpload.value = '';
            uploadForm.style.display = 'none';
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
// Helper function to show messages to the user
function showMessage(message, type) {
    const contributeTab = document.getElementById('contribute');
    const messageElement = document.createElement('div');
    messageElement.textContent = message;
    messageElement.className = 'message ${type}';
    contributeTab.appendChild(messageElement);
    
    // Remove the message after 5 seconds
    setTimeout(() => {
        messageElement.remove();
    }, 5000);
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
};

document.addEventListener('DOMContentLoaded', function() {
    const featureItems = document.querySelectorAll('.feature-item');

    // Use setTimeout to stagger the visibility for each feature item
    featureItems.forEach((item, index) => {
        setTimeout(() => {
            item.classList.add('visible');
        }, index * 200); // Stagger visibility by 200ms
    });
});

// JavaScript for handling subject click and fetching document
document.getElementById('login-form').addEventListener('submit', function (event) {
    event.preventDefault(); // Prevent the form from submitting

    // Get the email and password from the form
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    // Check if the email and password match the expected values
    if (email === "HannahCarl@brackenfell.edu" && password === "HC0057#st") {
        // Redirect to the moderation page (create moderation.html)
        window.location.href = "moderation.html"; // Change to the path of your moderation page
    } else {
        // Show an error message
        document.getElementById('error-message').textContent = "Invalid email or password.";
    }
});

// JavaScript for handling subject click and fetching document

document.querySelectorAll('.subject-block').forEach(subjectBlock => {
    subjectBlock.addEventListener('click', function() {
        const subject = this.textContent.trim(); // Get subject name

        // Define subject IDs (this can be expanded as per your need)
        const subjectIDs = {
            "Mathematics": "670ed342be670e75745fe650", // Maths ID
            // Add more subjects and their IDs if needed
        };

        if (subject in subjectIDs) {
            // Construct the API URL using the subject ID
            const apiUrl = `http://localhost:5281/api/Moderation/update/${subjectIDs[subject]}`;

            console.log('Fetching document for subject:', subject); // Debug log
            console.log('API URL:', apiUrl); // Debug log

            // Fetch the document for the clicked subject using PUT with authorization header
            fetch(apiUrl, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJTbG9hbmVDYXJseUBnaXJsc2hpZ2guZWR1IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6IkNhcmx5IFNsb2FuZSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6InRlYWNoZXIiLCJqdGkiOiJlNjE4NTkzYy0wNTE3LTRkMzEtYTU1Yy1hMzIzM2E5ZjI2NmMiLCJleHAiOjE3MjkwODU4MjEsImlzcyI6IlNoYXJlMlRlYWNoIiwiYXVkIjoiU2hhcmUyVGVhY2hVc2VycyJ9.0nvYPdsmUlv_MdtYMyt3I58z7zBnmjPMYnWy0xsjnmM' // Add your token here
                },
                body: JSON.stringify({ subject: subject }) // You can modify the body if needed
            })
            .then(response => {
                console.log('Response status:', response.status); // Debug log
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('Document for', subject, data); // Debug log
                document.getElementById('documentDisplay').innerHTML = `<pre>${JSON.stringify(data, null, 2)}</pre>`;
            })
            .catch(error => {
                console.error('Error fetching document:', error); // More detailed logging
                alert(`Failed to fetch document for ${subject}: ${error.message}`);
            });
        } else {
            alert('No document available for ' + subject);
        }
    });
});
