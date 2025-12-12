const API_BASE_URL = 'http://localhost:5000/api';

// Global backend preference (defaults to Entity Framework)
let useEntityFramework = true;

// Function to set backend preference
export const setBackendPreference = (useEF) => {
  useEntityFramework = useEF;
};

// Function to get backend preference
export const getBackendPreference = () => {
  return useEntityFramework;
};

// Helper to get headers with backend preference
const getHeaders = (additionalHeaders = {}) => {
  return {
    'Content-Type': 'application/json',
    'X-Use-EF': useEntityFramework.toString(),
    ...additionalHeaders
  };
};

export const authAPI = {
  login: async (cnic, password) => {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ cnic, password })
    });
    return response.json();
  }
};

export const citizenAPI = {
  register: async (data) => {
    const response = await fetch(`${API_BASE_URL}/citizen/register`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    return response.json();
  },

  getProfile: async (citizenID) => {
    const response = await fetch(`${API_BASE_URL}/citizen/profile/${citizenID}`, {
      headers: getHeaders()
    });
    return response.json();
  },

  getListings: async (citizenID) => {
    const response = await fetch(`${API_BASE_URL}/citizen/listings/${citizenID}`, {
      headers: getHeaders()
    });
    return response.json();
  },

  createListing: async (listing) => {
    const response = await fetch(`${API_BASE_URL}/citizen/listings`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(listing)
    });
    return response.json();
  },

  cancelListing: async (listingID, citizenID) => {
    const response = await fetch(`${API_BASE_URL}/citizen/listings/${listingID}/cancel`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ citizenID })
    });
    return response.json();
  },

  getTransactions: async (citizenID) => {
    const response = await fetch(`${API_BASE_URL}/citizen/transactions/${citizenID}`, {
      headers: getHeaders()
    });
    return response.json();
  },

  getCategories: async () => {
    const response = await fetch(`${API_BASE_URL}/citizen/categories`, {
      headers: getHeaders()
    });
    return response.json();
  },

  getAreas: async () => {
    const response = await fetch(`${API_BASE_URL}/citizen/areas`, {
      headers: getHeaders()
    });
    return response.json();
  },

  getPriceEstimate: async (categoryID, weight) => {
    const response = await fetch(`${API_BASE_URL}/citizen/price-estimate`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ categoryID, weight })
    });
    return response.json();
  }
};
