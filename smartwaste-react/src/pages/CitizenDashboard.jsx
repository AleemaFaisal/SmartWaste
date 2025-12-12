import { useState, useEffect } from 'react';
import { citizenAPI, setBackendPreference, getBackendPreference } from '../services/api';
import './CitizenDashboard.css';

export default function CitizenDashboard({ user, onLogout }) {
  const [activeTab, setActiveTab] = useState('create');
  const [profile, setProfile] = useState(null);
  const [listings, setListings] = useState([]);
  const [transactions, setTransactions] = useState([]);
  const [categories, setCategories] = useState([]);
  const [useEntityFramework, setUseEntityFramework] = useState(true);

  // Form state
  const [selectedCategory, setSelectedCategory] = useState('');
  const [weight, setWeight] = useState('');
  const [estimatedPrice, setEstimatedPrice] = useState(0);

  const [message, setMessage] = useState('');
  const [messageType, setMessageType] = useState(''); // 'success' or 'error'
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadProfile();
    loadCategories();
    loadListings();
    loadTransactions();
  }, []);

  useEffect(() => {
    if (selectedCategory && weight > 0) {
      calculatePrice();
    } else {
      setEstimatedPrice(0);
    }
  }, [selectedCategory, weight]);

  const handleBackendToggle = async () => {
    const newValue = !useEntityFramework;
    setUseEntityFramework(newValue);
    setBackendPreference(newValue);

    // Show switching message
    setMessage(`Switching to ${newValue ? 'Entity Framework (LINQ)' : 'Stored Procedures (SQL)'}...`);
    setMessageType('success');
    setLoading(true);

    // Refresh all data with new backend
    try {
      await Promise.all([
        loadProfile(),
        loadCategories(),
        loadListings(),
        loadTransactions()
      ]);

      setMessage(`✅ Successfully switched to ${newValue ? 'Entity Framework (LINQ)' : 'Stored Procedures (SQL)'}!`);
      setTimeout(() => setMessage(''), 3000);
    } catch (err) {
      setMessage('Error switching backend: ' + err.message);
      setMessageType('error');
    } finally {
      setLoading(false);
    }
  };

  const loadProfile = async () => {
    try {
      const data = await citizenAPI.getProfile(user.userID);
      setProfile(data);
    } catch (err) {
      console.error('Failed to load profile:', err);
    }
  };

  const loadCategories = async () => {
    try {
      const data = await citizenAPI.getCategories();
      setCategories(data);
    } catch (err) {
      console.error('Failed to load categories:', err);
    }
  };

  const loadListings = async () => {
    try {
      const data = await citizenAPI.getListings(user.userID);
      setListings(data);
    } catch (err) {
      console.error('Failed to load listings:', err);
    }
  };

  const loadTransactions = async () => {
    try {
      const data = await citizenAPI.getTransactions(user.userID);
      setTransactions(data);
    } catch (err) {
      console.error('Failed to load transactions:', err);
    }
  };

  const calculatePrice = async () => {
    try {
      const data = await citizenAPI.getPriceEstimate(parseInt(selectedCategory), parseFloat(weight));
      setEstimatedPrice(data.estimatedPrice);
    } catch (err) {
      console.error('Failed to calculate price:', err);
    }
  };

  const handleCreateListing = async (e) => {
    e.preventDefault();
    setMessage('');
    setLoading(true);

    try {
      const result = await citizenAPI.createListing({
        citizenID: user.userID,
        categoryID: parseInt(selectedCategory),
        weight: parseFloat(weight)
      });

      if (result.success) {
        const categoryName = categories.find(c => c.categoryID === parseInt(selectedCategory))?.categoryName;
        setMessage(`✅ Listing created successfully! Your ${categoryName} waste (${weight} kg) worth Rs. ${estimatedPrice.toFixed(2)} has been listed.`);
        setMessageType('success');

        // Reset form
        setSelectedCategory('');
        setWeight('');
        setEstimatedPrice(0);

        // Reload listings
        await loadListings();
      } else {
        setMessage('Failed to create listing');
        setMessageType('error');
      }
    } catch (err) {
      setMessage('Error creating listing: ' + err.message);
      setMessageType('error');
    } finally {
      setLoading(false);
    }
  };

  const handleCancelListing = async (listingID) => {
    if (!confirm('Are you sure you want to cancel this listing?')) return;

    try {
      const result = await citizenAPI.cancelListing(listingID, user.userID);
      if (result.success) {
        setMessage('Listing cancelled successfully');
        setMessageType('success');
        await loadListings();
      } else {
        setMessage('Failed to cancel listing');
        setMessageType('error');
      }
    } catch (err) {
      setMessage('Error: ' + err.message);
      setMessageType('error');
    }
  };

  return (
    <div className="dashboard">
      <div className="header">
        <div>
          <h1>SmartWaste Citizen Portal</h1>
          <p>{profile?.fullName || 'Loading...'}</p>
          <p className="location">📍 {profile?.areaName}</p>
        </div>
        <div className="header-actions">
          <div className="backend-toggle">
            <div className="toggle-label">Data Backend:</div>
            <div className="toggle-info">
              <span className="backend-name">
                {useEntityFramework ? 'Entity Framework (LINQ)' : 'Stored Procedures (SQL)'}
              </span>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={useEntityFramework}
                  onChange={handleBackendToggle}
                  disabled={loading}
                />
                <span className="slider"></span>
              </label>
              <div className="toggle-labels">
                <span className={useEntityFramework ? 'active' : ''}>EF</span>
                <span className={!useEntityFramework ? 'active' : ''}>SP</span>
              </div>
            </div>
            <div className="toggle-hint">Toggle to switch implementation</div>
          </div>
          <button onClick={onLogout} className="logout-btn">Logout</button>
        </div>
      </div>

      <div className="tabs">
        <button
          className={activeTab === 'create' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('create')}
        >
          📝 Sell Waste
        </button>
        <button
          className={activeTab === 'listings' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('listings')}
        >
          📋 My Listings ({listings.length})
        </button>
        <button
          className={activeTab === 'transactions' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('transactions')}
        >
          💰 Transactions ({transactions.length})
        </button>
        <button
          className={activeTab === 'profile' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('profile')}
        >
          👤 Profile
        </button>
      </div>

      {message && (
        <div className={`message ${messageType}`}>
          {message}
        </div>
      )}

      {activeTab === 'create' && (
        <div className="tab-content">
          <h2>Create New Waste Listing</h2>
          <form onSubmit={handleCreateListing}>
            <div className="form-group">
              <label>Waste Category</label>
              <select
                value={selectedCategory}
                onChange={(e) => setSelectedCategory(e.target.value)}
                required
              >
                <option value="">Select category...</option>
                {categories.map(cat => (
                  <option key={cat.categoryID} value={cat.categoryID}>
                    {cat.categoryName} - Rs. {cat.basePricePerKg}/kg
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Weight (kg)</label>
              <input
                type="number"
                step="0.1"
                min="0.1"
                placeholder="Enter weight"
                value={weight}
                onChange={(e) => setWeight(e.target.value)}
                required
              />
            </div>

            {estimatedPrice > 0 && (
              <div className="price-estimate">
                <h3>Estimated Earnings</h3>
                <div className="price">Rs. {estimatedPrice.toFixed(2)}</div>
                <p>You will receive this amount after collection</p>
              </div>
            )}

            <button type="submit" className="submit-btn" disabled={loading}>
              {loading ? 'Creating...' : 'Submit Listing'}
            </button>
          </form>
        </div>
      )}

      {activeTab === 'listings' && (
        <div className="tab-content">
          <div className="listings-header">
            <h2>Your Waste Listings</h2>
            <button onClick={loadListings} className="refresh-btn">🔄 Refresh</button>
          </div>

          {listings.length === 0 ? (
            <p className="no-data">No listings found. Create your first listing!</p>
          ) : (
            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Category</th>
                    <th>Weight (kg)</th>
                    <th>Price (Rs.)</th>
                    <th>Status</th>
                    <th>Date</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {listings.map(listing => (
                    <tr key={listing.listingID}>
                      <td>{listing.listingID}</td>
                      <td>{listing.categoryName}</td>
                      <td>{listing.weight.toFixed(2)}</td>
                      <td>{listing.estimatedPrice?.toFixed(2)}</td>
                      <td><span className={`status ${listing.status.toLowerCase()}`}>{listing.status}</span></td>
                      <td>{new Date(listing.createdAt).toLocaleDateString()}</td>
                      <td>
                        {listing.status === 'Pending' && (
                          <button
                            onClick={() => handleCancelListing(listing.listingID)}
                            className="cancel-btn"
                          >
                            Cancel
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'transactions' && (
        <div className="tab-content">
          <div className="listings-header">
            <h2>Transaction History</h2>
            <button onClick={loadTransactions} className="refresh-btn">🔄 Refresh</button>
          </div>

          {transactions.length === 0 ? (
            <p className="no-data">No transactions found.</p>
          ) : (
            <div className="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Transaction ID</th>
                    <th>Date</th>
                    <th>Amount (Rs.)</th>
                    <th>Payment Status</th>
                    <th>Payment Method</th>
                    <th>Operator</th>
                  </tr>
                </thead>
                <tbody>
                  {transactions.map(transaction => (
                    <tr key={`${transaction.transactionID}-${transaction.transactionDate}`}>
                      <td>{transaction.transactionID}</td>
                      <td>{new Date(transaction.transactionDate).toLocaleString()}</td>
                      <td>{transaction.totalAmount.toFixed(2)}</td>
                      <td>
                        <span className={`status ${transaction.paymentStatus.toLowerCase()}`}>
                          {transaction.paymentStatus}
                        </span>
                      </td>
                      <td>{transaction.paymentMethod || '-'}</td>
                      <td>{transaction.operatorID || 'Not assigned'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'profile' && (
        <div className="tab-content">
          <h2>My Profile</h2>

          {profile ? (
            <div className="profile-card">
              <div className="profile-row">
                <label>CNIC:</label>
                <span>{profile.citizenID}</span>
              </div>
              <div className="profile-row">
                <label>Full Name:</label>
                <span>{profile.fullName}</span>
              </div>
              <div className="profile-row">
                <label>Phone Number:</label>
                <span>{profile.phoneNumber}</span>
              </div>
              <div className="profile-row">
                <label>Area:</label>
                <span>{profile.areaName}</span>
              </div>
              <div className="profile-row">
                <label>City:</label>
                <span>{profile.city}</span>
              </div>
              <div className="profile-row">
                <label>Address:</label>
                <span>{profile.address}</span>
              </div>
              <div className="profile-row">
                <label>Member Since:</label>
                <span>{new Date(profile.memberSince).toLocaleDateString('en-US', {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric'
                })}</span>
              </div>
            </div>
          ) : (
            <p className="no-data">Loading profile...</p>
          )}
        </div>
      )}
    </div>
  );
}
