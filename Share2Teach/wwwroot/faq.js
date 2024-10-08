const faqListElement = document.getElementById("faq-list"); // Ensure this is defined globally
const errorMessageElement = document.getElementById("error-message"); // Assuming this element exists in your HTML
let hasFetched = false;

async function fetchFAQs() {
  if (hasFetched) return; // Prevent multiple calls
  hasFetched = true;

  try {
    const response = await fetch('http://localhost:5281/api/FAQ/list');

    if (response.ok) {
      const contentType = response.headers.get("content-type");

      if (contentType && contentType.includes("application/json")) {
        const faqs = await response.json();
        displayFAQs(faqs); // Call the function to display FAQs
      } else if (contentType && contentType.includes("text/html")) {
        const htmlContent = await response.text();
        if (faqListElement) {
          faqListElement.innerHTML = htmlContent;
        } else {
          displayError("FAQ list element not found.");
        }
      } else {
        displayError(`Unsupported content type: ${contentType}`);
      }
    } else {
      displayError(`Error fetching FAQs: ${response.statusText}`);
    }
  } catch (error) {
    displayError(`Error fetching FAQs: ${error.message}`);
  }
}

// Function to display FAQs
function displayFAQs(faqs) {
  if (faqListElement) {
    faqListElement.innerHTML = ''; // Clear previous content
    faqs.forEach(faq => {
      const faqItem = document.createElement('div');
      faqItem.classList.add('faq-item');
      faqItem.innerHTML = `
        <h2>${faq.question}</h2>
        <p>${faq.answer}</p>
      `;
      faqListElement.appendChild(faqItem);
    });
  } else {
    displayError("FAQ list element not found.");
  }
}

// Function to display error messages
function displayError(message) {
  if (errorMessageElement) {
    errorMessageElement.textContent = message; // Set error message
    errorMessageElement.style.display = 'block'; // Ensure it's visible
  } else {
    console.error("Error message element not found:", message);
  }
}

// Call the fetchFAQs function only once when the script loads
document.addEventListener('DOMContentLoaded', fetchFAQs);
