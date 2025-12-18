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

export const governmentAPI = {
  // ================================
  // WAREHOUSE ANALYTICS
  // ================================

  getWarehouseInventory: async (warehouseID = null) => {
    const url = warehouseID
      ? `${API_BASE_URL}/government/warehouse-inventory?warehouseID=${warehouseID}`
      : `${API_BASE_URL}/government/warehouse-inventory`;

    console.log("api fetching warehouseInventory: ", url)
    const response = await fetch(url, { headers: getHeaders() });
    console.log("api fetched warehouseInventory: ", response)
    return response.json();
  },

  getAllWarehouses: async () => {
    const response = await fetch(`${API_BASE_URL}/government/warehouses`, {
      headers: getHeaders()
    });
    return response.json();
  },

  // ================================
  // REPORTS
  // ================================

  analyzeHighYieldAreas: async (startDate = null, endDate = null) => {
    const response = await fetch(`${API_BASE_URL}/government/reports/high-yield`, {
      headers: getHeaders()
    });
    return response.json();
  },

  getOperatorPerformanceReport: async () => {
    const response = await fetch(`${API_BASE_URL}/government/reports/operator-performance`, {
      headers: getHeaders()
    });
    return response.json();
  },

  // ================================
  // CATEGORY MANAGEMENT
  // ================================

  getAllCategories: async () => {
    const response = await fetch(`${API_BASE_URL}/government/categories`, {
      headers: getHeaders()
    });
    return response.json();
  },

  createCategory: async (data) => {
    const response = await fetch(`${API_BASE_URL}/government/categories`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    return response.json();
  },

  updateCategoryPrice: async (categoryID, newPrice) => {
    const response = await fetch(`${API_BASE_URL}/government/categories/${categoryID}/price`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ newPrice })
    });
    return response.json();
  },

  deleteCategory: async (categoryID) => {
    const response = await fetch(`${API_BASE_URL}/government/categories/${categoryID}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    return response.json();
  },

  // ================================
  // OPERATOR MANAGEMENT
  // ================================

  createOperator: async (data) => {
    const response = await fetch(`${API_BASE_URL}/government/operators`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    return response.json();
  },

  assignOperatorToRoute: async (operatorID, routeID, warehouseID) => {
    const response = await fetch(`${API_BASE_URL}/government/operators/${operatorID}/assign`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ routeID, warehouseID })
    });
    return response.json();
  },

  deactivateOperator: async (operatorID) => {
    const response = await fetch(`${API_BASE_URL}/government/operators/deactivate/${operatorID}`, {
      method: 'PUT',
      headers: getHeaders()
    });
    return response.json();
  },

  getAllOperators: async () => {
    const response = await fetch(`${API_BASE_URL}/government/operators`, {
      headers: getHeaders()
    });
    return response.json();
  },

  // ================================
  // COMPLAINTS
  // ================================

  getAllComplaints: async (status = null) => {
    const url = status
      ? `${API_BASE_URL}/government/complaints?status=${encodeURIComponent(status)}`
      : `${API_BASE_URL}/government/complaints`;

    const response = await fetch(url, { headers: getHeaders() });
    return response.json();
  },

  updateComplaintStatus: async (complaintID, newStatus) => {
    const response = await fetch(`${API_BASE_URL}/government/complaints/${complaintID}/status`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ newStatus })
    });
    return response.json();
  },

  // ================================
  // ROUTES & AREAS
  // ================================

  getRoutes: async () => {
    const response = await fetch(`${API_BASE_URL}/government/routes`, {
      headers: getHeaders()
    });
    return response.json();
  },

  getAreas: async () => {
    const response = await fetch(`${API_BASE_URL}/government/areas`, {
      headers: getHeaders()
    });
    return response.json();
  }
};
