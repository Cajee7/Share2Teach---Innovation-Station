async function fetchFAQs() {
    const response = await fetch(`${apiUrl}/list`);
    const faqs = await response.json();
    const faqList = document.getElementById('faq-list');
    faqList.innerHTML = '';

    faqs.forEach(faq => {
        const faqItem = document.createElement('div');
        faqItem.className = 'faq-item';
        faqItem.innerHTML = `
            <strong>${faq.Question}</strong>
            <p>${faq.Answer}</p>
            <button class="update-btn" onclick="prepareUpdate('${faq._id}', '${faq.Question}', '${faq.Answer}')">
                <i class="fas fa-edit"></i> Update
            </button>
            <button class="delete-btn" onclick="deleteFAQ('${faq._id}')">
                <i class="fas fa-trash-alt"></i> Delete
            </button>
        `;
        faqList.appendChild(faqItem);
    });
}
