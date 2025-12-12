import { useState, useEffect } from "react";
import { operatorService } from "../services/api";
import "./OperatorDashboard.css";

export default function OperatorDashboard({
  user,
  onLogout,
  useEF,
  onToggleImplementation,
}) {
  const [activeTab, setActiveTab] = useState("route");
  const [profile, setProfile] = useState(null);
  const [routeItems, setRouteItems] = useState([]);
  const [history, setHistory] = useState([]);

  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("");
  const [loading, setLoading] = useState(false);

  // Modal state for collection
  const [showCollectionModal, setShowCollectionModal] = useState(false);
  const [selectedListing, setSelectedListing] = useState(null);
  const [collectionWeight, setCollectionWeight] = useState("");

  // Modal state for warehouse deposit
  const [showDepositModal, setShowDepositModal] = useState(false);
  const [depositCategory, setDepositCategory] = useState("");
  const [depositQuantity, setDepositQuantity] = useState("");
  const [categories] = useState([
    { id: 1, name: "Plastic" },
    { id: 2, name: "Paper" },
    { id: 3, name: "Metal" },
    { id: 4, name: "Glass" },
    { id: 5, name: "E-Waste" },
    { id: 6, name: "Organic" },
  ]);

  const loadAllData = async () => {
    setLoading(true);
    try {
      // Get operator ID from user (backend returns userID with capital I and D)
      const operatorID =
        user.operatorID || user.userId || user.userID || user.cnic;

      if (!operatorID) {
        console.error("User object:", user);
        throw new Error("Operator ID not found in user data");
      }

      // 1. Load Profile (for Operator Name, Route Name, Warehouse)
      const profileData = await operatorService.getDetails(operatorID);
      setProfile(profileData);

      // 2. Load Route (Pending Collections)
      const routeData = await operatorService.getCollectionPoints(operatorID);
      setRouteItems(routeData);

      // 3. Load History
      const historyData = await operatorService.getHistory(operatorID);
      setHistory(Array.isArray(historyData) ? historyData : []);
    } catch (err) {
      console.error("Load Error:", err);
      setMessage("Failed to load dashboard data. Is the backend running?");
      setMessageType("error");
      setHistory([]); // Ensure history is always an array
    } finally {
      setLoading(false);
    }
  };

  // Load data on mount or when Backend Mode changes
  useEffect(() => {
    loadAllData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [useEF]); // Re-run if toggle changes (loadAllData not in deps to avoid infinite loop)

  const handleCollect = (listingId, citizenName) => {
    setSelectedListing({ listingId, citizenName });
    setCollectionWeight("");
    setShowCollectionModal(true);
  };

  const handleConfirmCollection = async () => {
    if (!selectedListing) return;

    const weight = parseFloat(collectionWeight);
    if (isNaN(weight) || weight <= 0) {
      setMessage("Invalid weight entered");
      setMessageType("error");
      return;
    }

    setLoading(true);
    setShowCollectionModal(false);
    try {
      const operatorID =
        user.operatorID || user.userId || user.userID || user.cnic;
      const result = await operatorService.collectWaste({
        operatorID: operatorID,
        listingID: selectedListing.listingId,
        collectedWeight: weight,
        warehouseID: profile?.warehouseID || 1,
      });

      if (result.success) {
        // Build success message with payment info if available
        let successMsg = `✅ Collection recorded for ${selectedListing.citizenName}!`;
        if (result.paymentAmount && result.verificationCode) {
          successMsg += `\n💰 Payment: Rs.${result.paymentAmount.toFixed(
            2
          )} (Pending)\n🔑 Code: ${result.verificationCode}`;
        }
        setMessage(successMsg);
        setMessageType("success");
        setSelectedListing(null);
        setCollectionWeight("");
        loadAllData();
        // Auto-dismiss success message after 4 seconds
        setTimeout(() => {
          setMessage("");
          setMessageType("");
        }, 4000);
      } else {
        setMessage("❌ Failed: " + result.message);
        setMessageType("error");
        // Auto-dismiss error message after 5 seconds
        setTimeout(() => {
          setMessage("");
          setMessageType("");
        }, 5000);
      }
    } catch (err) {
      setMessage("❌ Error: " + err.message);
      setMessageType("error");
      // Auto-dismiss error message after 5 seconds
      setTimeout(() => {
        setMessage("");
        setMessageType("");
      }, 5000);
    } finally {
      setLoading(false);
    }
  };

  const handleDeposit = async () => {
    const quantity = parseFloat(depositQuantity);
    const categoryId = parseInt(depositCategory);

    if (isNaN(categoryId) || categoryId <= 0) {
      setMessage("Please select a category");
      setMessageType("error");
      return;
    }

    if (isNaN(quantity) || quantity <= 0) {
      setMessage("Invalid quantity entered");
      setMessageType("error");
      return;
    }

    setLoading(true);
    setShowDepositModal(false);
    try {
      const result = await operatorService.depositWaste({
        warehouseID: profile?.warehouseID || 1,
        categoryID: categoryId,
        quantity: quantity,
      });

      if (result.success) {
        setMessage(`✅ ${quantity}kg of waste deposited at warehouse!`);
        setMessageType("success");
        setDepositCategory("");
        setDepositQuantity("");
        // Auto-dismiss success message after 4 seconds
        setTimeout(() => {
          setMessage("");
          setMessageType("");
        }, 4000);
      } else {
        setMessage("❌ Deposit failed: " + result.message);
        setMessageType("error");
        // Auto-dismiss error message after 5 seconds
        setTimeout(() => {
          setMessage("");
          setMessageType("");
        }, 5000);
      }
    } catch (err) {
      setMessage("❌ Error: " + err.message);
      setMessageType("error");
      // Auto-dismiss error message after 5 seconds
      setTimeout(() => {
        setMessage("");
        setMessageType("");
      }, 5000);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="dashboard">
      {/* HEADER */}
      <div className="header">
        <div>
          <h1>SmartWaste Operator Portal</h1>
          <p>{profile?.fullName || "Loading..."}</p>
          <div className="location">
            <span>🚛 Route: {profile?.route?.routeName || "Not Assigned"}</span>
            <span>•</span>
            <span>
              🏭 Warehouse: {profile?.warehouse?.warehouseName || "..."}
            </span>
          </div>
        </div>

        <div className="header-actions">
          {/* BACKEND TOGGLE (Same as Citizen) */}
          <div className="backend-toggle">
            <div className="toggle-label">Backend Mode</div>
            <div className="toggle-info">
              <span
                style={{ color: "white", fontSize: "12px", marginRight: "5px" }}
              >
                {useEF ? "EF Core" : "Stored Proc"}
              </span>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={useEF}
                  onChange={onToggleImplementation}
                  disabled={loading}
                />
                <span className="slider"></span>
              </label>
            </div>
          </div>
          <button onClick={onLogout} className="logout-btn">
            Logout
          </button>
        </div>
      </div>

      {/* TABS */}
      <div className="tabs">
        <button
          className={`tab ${activeTab === "route" ? "active" : ""}`}
          onClick={() => setActiveTab("route")}
        >
          📍 Active Route ({routeItems.length})
        </button>
        <button
          className={`tab ${activeTab === "history" ? "active" : ""}`}
          onClick={() => setActiveTab("history")}
        >
          📜 Collection History
        </button>
        <button
          className={`tab ${activeTab === "profile" ? "active" : ""}`}
          onClick={() => setActiveTab("profile")}
        >
          👤 My Profile
        </button>
        <button
          className={`tab ${activeTab === "warehouse" ? "active" : ""}`}
          onClick={() => setActiveTab("warehouse")}
        >
          🏭 Warehouse Deposit
        </button>
      </div>

      {/* MESSAGE BOX */}
      {message && <div className={`message ${messageType}`}>{message}</div>}

      {/* TAB 1: ACTIVE ROUTE */}
      {activeTab === "route" && (
        <div className="tab-content">
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              marginBottom: "20px",
            }}
          >
            <h2>Pending Collections</h2>
            <button onClick={loadAllData} className="refresh-btn">
              🔄 Refresh
            </button>
          </div>

          {routeItems.length === 0 ? (
            <p
              className="no-data"
              style={{ textAlign: "center", padding: "40px", color: "#999" }}
            >
              No pending pickups assigned to your route.
            </p>
          ) : (
            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Listing ID</th>
                    <th>Citizen Name</th>
                    <th>Address</th>
                    <th>Category</th>
                    <th>Est. Weight</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {routeItems.map((item) => (
                    <tr key={item.listingID}>
                      <td>#{item.listingID}</td>
                      <td>{item.citizenName}</td>
                      <td>{item.address}</td>
                      <td>
                        <span
                          style={{
                            background: "#E3F2FD",
                            color: "#1565C0",
                            padding: "2px 8px",
                            borderRadius: "10px",
                            fontSize: "12px",
                          }}
                        >
                          {item.categoryName || item.category}
                        </span>
                      </td>
                      <td>{item.weight || item.estimatedWeight} kg</td>
                      <td>
                        <button
                          className="action-btn"
                          onClick={() =>
                            handleCollect(item.listingID, item.citizenName)
                          }
                        >
                          Collect
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: HISTORY */}
      {activeTab === "history" && (
        <div className="tab-content">
          <h2>Past Collections</h2>
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Listing ID</th>
                  <th>Collected Weight</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {history.length === 0 ? (
                  <tr>
                    <td colSpan="4" style={{ textAlign: "center" }}>
                      No history found
                    </td>
                  </tr>
                ) : (
                  history.map((item, idx) => (
                    <tr key={idx}>
                      <td>
                        {new Date(item.collectedDate).toLocaleDateString()}
                      </td>
                      <td>#{item.listingID}</td>
                      <td style={{ fontWeight: "bold" }}>
                        {item.collectedWeight} kg
                      </td>
                      <td style={{ color: "green" }}>✔ Verified</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: PROFILE */}
      {activeTab === "profile" && (
        <div className="tab-content">
          <h2>Operator Profile</h2>
          {profile ? (
            <div className="profile-card">
              <div className="profile-row">
                <label>Operator ID:</label>
                <span>{profile.operatorID}</span>
              </div>
              <div className="profile-row">
                <label>Full Name:</label>
                <span>{profile.fullName}</span>
              </div>
              <div className="profile-row">
                <label>Phone:</label>
                <span>{profile.phoneNumber}</span>
              </div>
              <div className="profile-row">
                <label>Route:</label>
                <span>{profile.route?.routeName}</span>
              </div>
              <div className="profile-row">
                <label>Warehouse:</label>
                <span>{profile.warehouse?.warehouseName}</span>
              </div>
              <div className="profile-row">
                <label>Status:</label>
                <span style={{ color: "green" }}>{profile.status}</span>
              </div>
            </div>
          ) : (
            <p>Loading...</p>
          )}
        </div>
      )}

      {/* TAB 4: WAREHOUSE DEPOSIT */}
      {activeTab === "warehouse" && (
        <div className="tab-content">
          <h2>Warehouse Deposit</h2>
          <p style={{ marginBottom: "20px", color: "#666" }}>
            Deposit collected waste at{" "}
            <strong>{profile?.warehouse?.warehouseName || "warehouse"}</strong>
          </p>

          <button
            className="btn"
            onClick={() => setShowDepositModal(true)}
            style={{
              padding: "12px 24px",
              fontSize: "16px",
              background: "#2e7d32",
            }}
          >
            🏭 Record Warehouse Deposit
          </button>
        </div>
      )}

      {/* COLLECTION MODAL */}
      {showCollectionModal && selectedListing && (
        <div
          className="modal-overlay"
          onClick={() => setShowCollectionModal(false)}
        >
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Record Collection</h3>
            <p style={{ marginBottom: "20px", color: "#666" }}>
              Collecting from: <strong>{selectedListing.citizenName}</strong>
            </p>

            <div className="form-group">
              <label>Actual Weight (kg):</label>
              <input
                type="number"
                step="0.1"
                min="0"
                value={collectionWeight}
                onChange={(e) => setCollectionWeight(e.target.value)}
                placeholder="e.g. 5.5"
                autoFocus
                onKeyPress={(e) => {
                  if (e.key === "Enter") {
                    handleConfirmCollection();
                  }
                }}
              />
            </div>

            <div style={{ display: "flex", gap: "10px", marginTop: "20px" }}>
              <button
                className="btn"
                onClick={handleConfirmCollection}
                disabled={loading}
              >
                Confirm Collection
              </button>
              <button
                className="btn btn--ghost"
                onClick={() => {
                  setShowCollectionModal(false);
                  setSelectedListing(null);
                  setCollectionWeight("");
                }}
                disabled={loading}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* WAREHOUSE DEPOSIT MODAL */}
      {showDepositModal && (
        <div
          className="modal-overlay"
          onClick={() => setShowDepositModal(false)}
        >
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Warehouse Deposit</h3>
            <p style={{ marginBottom: "20px", color: "#666" }}>
              Depositing at:{" "}
              <strong>{profile?.warehouse?.warehouseName}</strong>
            </p>

            <div className="form-group">
              <label>Waste Category:</label>
              <select
                value={depositCategory}
                onChange={(e) => setDepositCategory(e.target.value)}
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "5px",
                  border: "1px solid #ddd",
                  fontSize: "14px",
                }}
              >
                <option value="">-- Select Category --</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ marginTop: "15px" }}>
              <label>Quantity (kg):</label>
              <input
                type="number"
                step="0.1"
                min="0"
                value={depositQuantity}
                onChange={(e) => setDepositQuantity(e.target.value)}
                placeholder="e.g. 25.5"
                onKeyPress={(e) => {
                  if (e.key === "Enter") {
                    handleDeposit();
                  }
                }}
              />
            </div>

            <div style={{ display: "flex", gap: "10px", marginTop: "20px" }}>
              <button
                className="btn"
                onClick={handleDeposit}
                disabled={loading}
              >
                Confirm Deposit
              </button>
              <button
                className="btn btn--ghost"
                onClick={() => {
                  setShowDepositModal(false);
                  setDepositCategory("");
                  setDepositQuantity("");
                }}
                disabled={loading}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
