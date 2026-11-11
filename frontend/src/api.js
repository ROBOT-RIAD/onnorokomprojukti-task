const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export async function apiRequest(endpoint, options = {}) {
  const { headers = {}, ...restOptions } = options;
  const url = `${API_BASE_URL.replace(/\/$/, '')}${endpoint}`;

  let response;
  try {
    response = await fetch(url, {
      ...restOptions,
      headers: {
        'Content-Type': 'application/json',
        ...headers,
      },
    });
  } catch (err) {
    throw new Error('সার্ভারের সাথে সংযোগ করা যাচ্ছে না। ব্যাকএন্ড সার্ভার চালু আছে কিনা পরীক্ষা করুন। (Could not connect to backend server)');
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get('content-type');
  const isJson = contentType && contentType.includes('application/json');
  const data = isJson ? await response.json() : await response.text();

  if (!response.ok) {
    const errorMsg = (typeof data === 'object' && data?.error) ? data.error : (typeof data === 'string' ? data : 'অনাকাঙ্ক্ষিত সমস্যা দেখা দিয়েছে');
    const error = new Error(errorMsg);
    error.statusCode = response.status;
    error.data = data;
    throw error;
  }

  return data;
}

// 1. POST /classes
export function createClass(data) {
  return apiRequest('/classes', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// 2. GET /classes
export function getClasses() {
  return apiRequest('/classes', {
    method: 'GET',
  });
}

// 3. GET /classes/{classId}
export function getClassById(classId) {
  return apiRequest(`/classes/${classId}`, {
    method: 'GET',
  });
}

// 4. POST /classes/{classId}/holds
export function createHold(classId, studentId) {
  return apiRequest(`/classes/${classId}/holds`, {
    method: 'POST',
    headers: {
      'X-Student-Id': studentId,
    },
  });
}

// 5. GET /holds/{holdId}
export function getHoldStatus(holdId) {
  return apiRequest(`/holds/${holdId}`, {
    method: 'GET',
  });
}

// 6. POST /holds/{holdId}/confirm
export function confirmHold(holdId, studentId) {
  return apiRequest(`/holds/${holdId}/confirm`, {
    method: 'POST',
    headers: {
      'X-Student-Id': studentId,
    },
  });
}

// 7. DELETE /holds/{holdId}
export function cancelHold(holdId, studentId) {
  return apiRequest(`/holds/${holdId}`, {
    method: 'DELETE',
    headers: {
      'X-Student-Id': studentId,
    },
  });
}

// 8. GET /me/holds
export function getMyHolds(studentId) {
  return apiRequest('/me/holds', {
    method: 'GET',
    headers: {
      'X-Student-Id': studentId,
    },
  });
}

export { API_BASE_URL };
