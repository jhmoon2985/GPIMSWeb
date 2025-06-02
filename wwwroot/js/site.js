function showNotification(message, type = 'info', duration = 5000) {
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show notification-alert" role="alert">
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'danger' ? 'exclamation-triangle' : 'info-circle'}"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    
    // Create notification container if it doesn't exist
    let container = document.getElementById('notification-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'notification-container';
        container.style.position = 'fixed';
        container.style.top = '20px';
        container.style.right = '20px';
        container.style.zIndex = '9999';
        container.style.width = '300px';
        document.body.appendChild(container);
    }
    
    // Add notification
    const div = document.createElement('div');
    div.innerHTML = alertHtml;
    container.appendChild(div.firstElementChild);
    
    // Auto remove after duration
    setTimeout(() => {
        const alert = container.querySelector('.notification-alert');
        if (alert) {
            alert.remove();
        }
    }, duration);
}

// Confirm dialog wrapper
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// Format numbers with localization
function formatNumber(number, decimals = 2) {
    return number.toLocaleString(undefined, {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    });
}

// Format date with localization
function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString();
}

// AJAX error handler
function handleAjaxError(xhr, status, error) {
    console.error('AJAX Error:', status, error);
    showNotification('An error occurred. Please try again.', 'danger');
}

// Setup global AJAX error handling
$(document).ready(function() {
    $.ajaxSetup({
        error: handleAjaxError
    });
    
    // Add CSRF token to all AJAX requests
    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({
            beforeSend: function(xhr) {
                xhr.setRequestHeader('RequestVerificationToken', token);
            }
        });
    }
});

// Data refresh utilities
let refreshInterval;

function startAutoRefresh(intervalMs = 30000) {
    stopAutoRefresh();
    refreshInterval = setInterval(() => {
        refreshData();
    }, intervalMs);
}

function stopAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}

function refreshData() {
    // Override this function in specific pages
    console.log('Refreshing data...');
}

// Visibility change handler for auto-refresh
document.addEventListener('visibilitychange', function() {
    if (document.hidden) {
        stopAutoRefresh();
    } else {
        startAutoRefresh();
    }
});