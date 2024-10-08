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
}

async function performSearch() {
    const query = document.getElementById('searchInput').value;
    if (!query) {
        showError('Please enter a search term.');
        return;
    }

    try {
        const response = await fetch(`YOUR_API_ENDPOINT?search=${query}`);
        if (!response.ok) throw new Error('Network response was not ok');

        const results = await response.json();
        renderSearchResults(results);
    } catch (error) {
        showError('Error fetching search results. Please try again later.');
    }
}

function showError(message) {
    const errorMessage = document.getElementById('error-message');
    errorMessage.innerText = message;
    errorMessage.style.display = 'block';
}

function renderSearchResults(results) {
    // Logic to display search results
    // Add your code to handle displaying results here
}
