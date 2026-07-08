// ===== API BASE =====
const API_BASE = '/api/v1';

function getToken() {
  return localStorage.getItem('token');
}

async function apiFetch(path, options = {}) {
  const token = getToken();
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(API_BASE + path, { ...options, headers });

  if (res.status === 401) {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    window.location.href = '/login.html';
    return;
  }

  const json = await res.json().catch(() => null);

  if (!res.ok) {
    const msg = json?.message || `HTTP ${res.status}`;
    throw new Error(msg);
  }

  return json;
}

// ===== AUTH =====
const Auth = {
  async login(username, password) {
    return apiFetch('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password })
    });
  },
  async register(username, email, password, confirmPassword) {
    return apiFetch('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ username, email, password, confirmPassword })
    });
  }
};

// ===== TASKS =====
const Tasks = {
  async getAll() {
    return apiFetch('/tasks');
  },
  async getPaged(page = 1, pageSize = 10, status = null) {
    let qs = `?page=${page}&pageSize=${pageSize}`;
    if (status) qs += `&status=${status}`;
    return apiFetch('/tasks/paged' + qs);
  },
  async getById(id) {
    return apiFetch(`/tasks/${id}`);
  },
  async create(data) {
    return apiFetch('/tasks', { method: 'POST', body: JSON.stringify(data) });
  },
  async update(id, data) {
    return apiFetch(`/tasks/${id}`, { method: 'PUT', body: JSON.stringify({ ...data, id }) });
  },
  async delete(id) {
    return apiFetch(`/tasks/${id}`, { method: 'DELETE' });
  },
  async activate(id) {
    return apiFetch(`/tasks/${id}/activate`, { method: 'POST' });
  },
  async pause(id) {
    return apiFetch(`/tasks/${id}/pause`, { method: 'POST' });
  },
  async resume(id) {
    return apiFetch(`/tasks/${id}/resume`, { method: 'POST' });
  },
  async trigger(id) {
    return apiFetch(`/tasks/${id}/trigger`, { method: 'POST' });
  },
  async getLogs(id) {
    return apiFetch(`/tasks/${id}/logs`);
  }
};

// ===== DASHBOARD =====
const Dashboard = {
  async get() {
    return apiFetch('/dashboard');
  }
};

// ===== TOAST =====
function toast(message, type = 'info') {
  let container = document.querySelector('.toast-container');
  if (!container) {
    container = document.createElement('div');
    container.className = 'toast-container';
    document.body.appendChild(container);
  }
  const icons = { success: '✅', error: '❌', info: 'ℹ️', warning: '⚠️' };
  const el = document.createElement('div');
  el.className = `toast ${type}`;
  el.innerHTML = `<span class="toast-icon">${icons[type] || 'ℹ️'}</span><span>${message}</span>`;
  container.appendChild(el);
  setTimeout(() => el.remove(), 3500);
}

// ===== BADGE =====
function statusBadge(status) {
  const s = (status || '').toLowerCase();
  return `<span class="badge badge-${s}">${status}</span>`;
}

// ===== GUARD =====
function requireAuth() {
  if (!getToken()) {
    window.location.href = '/login.html';
  }
}

function setUsername() {
  const el = document.getElementById('username-display');
  if (el) el.textContent = localStorage.getItem('username') || 'User';
  const av = document.getElementById('user-avatar');
  if (av) av.textContent = (localStorage.getItem('username') || 'U')[0].toUpperCase();
}

function logout() {
  localStorage.removeItem('token');
  localStorage.removeItem('username');
  window.location.href = '/login.html';
}
