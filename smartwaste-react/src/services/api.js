const API_BASE_URL = "http://localhost:5000/api";
const OPERATOR_BASE_URL = `${API_BASE_URL}/operator`;

// Global backend preference (defaults to Entity Framework)
// Read from localStorage if available
let useEntityFramework =
  localStorage.getItem("useEF") === "false" ? false : true;
console.log(`[API] Initial backend mode: ${useEntityFramework ? "EF" : "SP"}`);

// Function to set backend preference
export const setBackendPreference = (useEF) => {
  useEntityFramework = useEF;
  console.log(
    `[API] Backend mode changed to: ${
      useEF ? "EF (Entity Framework)" : "SP (Stored Procedures)"
    } - X-Use-EF: ${useEF}`
  );
};

// Function to get backend preference
export const getBackendPreference = () => {
  return useEntityFramework;
};

// Helper to get headers with backend preference
const getHeaders = (additionalHeaders = {}) => {
  return {
    "Content-Type": "application/json",
    "X-Use-EF": useEntityFramework.toString(),
    ...additionalHeaders,
  };
};

export const authAPI = {
  login: async (cnic, password) => {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: "POST",
      headers: getHeaders(),
      body: JSON.stringify({ cnic, password }),
    });
    return response.json();
  },
};

export const citizenAPI = {
  register: async (data) => {
    const response = await fetch(`${API_BASE_URL}/citizen/register`, {
      method: "POST",
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    return response.json();
  },

  getProfile: async (citizenID) => {
    const response = await fetch(
      `${API_BASE_URL}/citizen/profile/${citizenID}`,
      {
        headers: getHeaders(),
      }
    );
    return response.json();
  },

  getListings: async (citizenID) => {
    const response = await fetch(
      `${API_BASE_URL}/citizen/listings/${citizenID}`,
      {
        headers: getHeaders(),
      }
    );
    return response.json();
  },

  createListing: async (listing) => {
    const response = await fetch(`${API_BASE_URL}/citizen/listings`, {
      method: "POST",
      headers: getHeaders(),
      body: JSON.stringify(listing),
    });
    return response.json();
  },

  cancelListing: async (listingID, citizenID) => {
    const response = await fetch(
      `${API_BASE_URL}/citizen/listings/${listingID}/cancel`,
      {
        method: "PUT",
        headers: getHeaders(),
        body: JSON.stringify({ citizenID }),
      }
    );
    return response.json();
  },

  getTransactions: async (citizenID) => {
    const response = await fetch(
      `${API_BASE_URL}/citizen/transactions/${citizenID}`,
      {
        headers: getHeaders(),
      }
    );
    return response.json();
  },

  getCategories: async () => {
    const response = await fetch(`${API_BASE_URL}/citizen/categories`, {
      headers: getHeaders(),
    });
    return response.json();
  },

  getAreas: async () => {
    const response = await fetch(`${API_BASE_URL}/citizen/areas`, {
      headers: getHeaders(),
    });
    return response.json();
  },

  getPriceEstimate: async (categoryID, weight) => {
    const response = await fetch(`${API_BASE_URL}/citizen/price-estimate`, {
      method: "POST",
      headers: getHeaders(),
      body: JSON.stringify({ categoryID, weight }),
    });
    return response.json();
  },
};

export const operatorService = {
  // 1. Get Profile & Route Info
  getDetails: async (operatorId) => {
    const res = await fetch(`${OPERATOR_BASE_URL}/details/${operatorId}`, {
      headers: getHeaders(),
    });
    return res.json();
  },

  // 2. Get Pending Collections (The Route)
  getCollectionPoints: async (operatorId) => {
    const res = await fetch(`${OPERATOR_BASE_URL}/collections/${operatorId}`, {
      headers: getHeaders(),
    });
    return res.json();
  },

  // 3. Submit a Collection (Perform Collection)
  collectWaste: async (data) => {
    /* Expected data format:
      {
        operatorID: "...",
        listingID: 123,
        collectedWeight: 5.5,
        warehouseID: 1
      }
    */
    const res = await fetch(`${OPERATOR_BASE_URL}/collect`, {
      method: "POST",
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    return res.json();
  },

  // 4. Deposit Waste to Warehouse
  depositWaste: async (data) => {
    const res = await fetch(`${OPERATOR_BASE_URL}/deposit`, {
      method: "POST",
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    return res.json();
  },

  // 5. View History
  getHistory: async (operatorId) => {
    const res = await fetch(`${OPERATOR_BASE_URL}/history/${operatorId}`, {
      headers: getHeaders(),
    });
    return res.json();
  },

  // 6. View Performance Stats
  getPerformance: async (operatorId) => {
    const res = await fetch(`${OPERATOR_BASE_URL}/performance/${operatorId}`, {
      headers: getHeaders(),
    });
    return res.json();
  },
};
