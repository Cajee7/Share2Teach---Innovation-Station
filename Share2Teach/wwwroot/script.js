function openTab(event, tabName) {
    const tabContents = document.querySelectorAll('.tab-content');
    const tabButtons = document.querySelectorAll('.tab-button');

    // Hide all tab content
    tabContents.forEach(content => {
        content.classList.remove('active');
    });

    // Remove active class from all tab buttons
    tabButtons.forEach(button => {
        button.classList.remove('active');
    });

    // Show the selected tab content and add active class to button
    document.getElementById(tabName).classList.add('active');
    event.currentTarget.classList.add('active');
}
