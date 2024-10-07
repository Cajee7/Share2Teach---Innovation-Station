const faqForm = document.getElementById('faq-form');
const errorMessage = document.getElementById('error-message');
const faqList = document.getElementById('faq-list');

// Fetch all FAQs on page load
document.addEventListener('DOMContentLoaded', fetchFAQs);

faqForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    const question = document.getElementById('question').value;
    const answer = document.getElementById('answer').value;

    try {
        const response = await fetch('/api/faq/add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ question, answer })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Something went wrong!');
        }

        // Clear form and fetch updated FAQ list
        faqForm.reset();
        fetchFAQs();
        displayErrorMessage('FAQ added successfully!', false);

    } catch (error) {
        displayErrorMessage(error.message);
    }
});

// Function to fetch FAQs
async function fetchFAQs() {
    try {
        const response = await fetch('/api/faq/list');

        if (!response.ok) {
            throw new Error('Could not fetch FAQs.');
        }

        const faqs = await response.json();
        displayFAQs(faqs);
    } catch (error) {
        displayErrorMessage(error.message);
    }
}

// Function to display FAQs
function displayFAQs(faqs) {
    faqList.innerHTML = '';
    faqs.forEach(faq => {
        const faqItem = document.createElement('div');
        faqItem.classList.add('faq-item');
        faqItem.innerHTML = `<strong>Q: ${faq.Question}</strong><br>A: ${faq.Answer}`;
        faqList.appendChild(faqItem);
    });
}

// Function to display error messages
function displayErrorMessage(message, isError = true) {
    errorMessage.innerText = message;
    errorMessage.style.color = isError ? 'red' : 'green';
}
