document.addEventListener('submit', e => {
 const message=e.target.dataset.confirm;
 if(message && !window.confirm(message)) e.preventDefault();
});
document.addEventListener('click', e => { if(e.target.closest('[data-print]')) window.print(); });
