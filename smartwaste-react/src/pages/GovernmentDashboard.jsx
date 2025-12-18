import { useState, useEffect } from 'react';
import { governmentAPI, setBackendPreference } from '../services/api';
import { LineChart, Line, BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import React from 'react';
import './GovernmentDashboard.css';

export default function GovernmentDashboard({ user, onLogout }) {
    const [activeTab, setActiveTab] = useState('warehouses');
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState('');
    const [messageType, setMessageType] = useState('');
    const [useEntityFramework, setUseEntityFramework] = useState(true);

    // Warehouse Data
    const [warehouses, setWarehouses] = useState([]);
    const [selectedWarehouse, setSelectedWarehouse] = useState('');
    const [warehouseInventory, setWarehouseInventory] = useState([]);

    // Reports Data
    const [highYieldAreas, setHighYieldAreas] = useState([]);
    const [operatorPerformance, setOperatorPerformance] = useState([]);
    const [dateRange, setDateRange] = useState({ start: '', end: '' });

    // Category Management
    const [categories, setCategories] = useState([]);
    const [categoryForm, setCategoryForm] = useState({
        categoryName: '',
        basePricePerKg: '',
        description: ''
    });
    const [editingCategory, setEditingCategory] = useState(null);
    const [newPrice, setNewPrice] = useState("");
    // Operator Management
    const [operators, setOperators] = useState([]);
    const [routes, setRoutes] = useState([]);
    const [operatorForm, setOperatorForm] = useState({
        cnic: '',
        fullName: '',
        phoneNumber: '',
        routeID: '',
        warehouseID: ''
    });

    // Complaints
    const [complaints, setComplaints] = useState([]);
    const [complaintFilter, setComplaintFilter] = useState('all');
    const [expandedComplaints, setExpandedComplaints] = React.useState({});

    const toggleExpanded = (id) => {
        setExpandedComplaints(prev => ({
            ...prev,
            [id]: !prev[id]
        }));
    };
    useEffect(() => {
        loadInitialData();
    }, []);

    const loadInitialData = async () => {
        try {
            await loadWarehouses();
            await loadCategories();
            await loadOperators();
            await loadRoutes();
            await loadComplaints();
        } catch (err) {
            console.error('Failed to load initial data:', err);
        }
    };

    const showMessage = (text, type = 'success') => {
        setMessage(text);
        setMessageType(type);
        setTimeout(() => setMessage(''), 5000);
    };

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
            await loadInitialData();

            setMessage(`✅ Successfully switched to ${newValue ? 'Entity Framework (LINQ)' : 'Stored Procedures (SQL)'}!`);
            setTimeout(() => setMessage(''), 3000);
        } catch (err) {
            setMessage('Error switching backend: ' + err.message);
            setMessageType('error');
        } finally {
            setLoading(false);
        }
    };

    // ================================
    // WAREHOUSE FUNCTIONS
    // ================================

    const loadWarehouses = async () => {
        try {
            const data = await governmentAPI.getAllWarehouses();
            setWarehouses(data);
            console.log("set all warehouses: ", data);
        } catch (err) {
            console.error('Failed to load warehouses:', err);
        }
    };

    const loadWarehouseInventory = async (warehouseID = null) => {
        try {
            setLoading(true);
            const data = await governmentAPI.getWarehouseInventory(warehouseID);
            setWarehouseInventory(data);
            console.log("set warehouse inventory: ", data);
        } catch (err) {
            console.error('Failed to load inventory:', err);
            showMessage('Failed to load inventory', 'error');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (activeTab === 'warehouses') {
            loadWarehouseInventory(selectedWarehouse || null);
        }
    }, [selectedWarehouse, activeTab]);

    // ================================
    // REPORTS FUNCTIONS
    // ================================

    const loadHighYieldReport = async () => {
        try {
            setLoading(true);
            const data = await governmentAPI.analyzeHighYieldAreas();
            setHighYieldAreas(data);
            console.log("high yield areas: ", data);
        } catch (err) {
            console.error('Failed to load report:', err);
            showMessage('Failed to generate report', 'error');
        } finally {
            setLoading(false);
        }
    };

    const loadOperatorReport = async () => {
        try {
            setLoading(true);
            const data = await governmentAPI.getOperatorPerformanceReport();
            setOperatorPerformance(data);
        } catch (err) {
            console.error('Failed to load operator report:', err);
            showMessage('Failed to load operator performance', 'error');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (activeTab === 'reports') {
            loadHighYieldReport();
            loadOperatorReport();
        }
    }, [activeTab]);

    // ================================
    // CATEGORY FUNCTIONS
    // ================================

    const loadCategories = async () => {
        try {
            const data = await governmentAPI.getAllCategories();
            setCategories(data);
            console.log("categories loaded: ", data);
        } catch (err) {
            console.error('Failed to load categories:', err);
        }
    };

    const handleCreateCategory = async (e) => {
        e.preventDefault();
        try {
            setLoading(true);
            const result = await governmentAPI.createCategory({
                categoryName: categoryForm.categoryName,
                basePricePerKg: parseFloat(categoryForm.basePricePerKg),
                description: categoryForm.description || null
            });

            if (result.success) {
                showMessage('✅ Category created successfully!');
                setCategoryForm({ categoryName: '', basePricePerKg: '', description: '' });
                await loadCategories();
            } else {
                showMessage(result.message || 'Failed to create category', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleUpdatePrice = async (categoryID, newPrice) => {
        if (!newPrice || isNaN(newPrice)) return;

        try {
            setLoading(true);
            const result = await governmentAPI.updateCategoryPrice(categoryID, parseFloat(newPrice));

            if (result.success) {
                showMessage('✅ Price updated successfully!');
                await loadCategories();
            } else {
                showMessage(result.message || 'Failed to update price', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteCategory = async (categoryID, categoryName) => {
        if (!confirm(`Are you sure you want to delete "${categoryName}"? This cannot be undone.`)) return;

        try {
            setLoading(true);
            const result = await governmentAPI.deleteCategory(categoryID);

            if (result.success) {
                showMessage('✅ Category deleted successfully!');
                await loadCategories();
            } else {
                showMessage(result.message || 'Failed to delete category', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    // ================================
    // OPERATOR FUNCTIONS
    // ================================

    const loadOperators = async () => {
        try {
            console.log("fetching operators")
            const data = await governmentAPI.getAllOperators();
            setOperators(data);
            console.log("loaded operators: ", data);
        } catch (err) {
            console.error('Failed to load operators:', err);
        }
    };

    const loadRoutes = async () => {
        try {
            const data = await governmentAPI.getRoutes();
            setRoutes(data);
            console.log("loaded routes: ", data);
        } catch (err) {
            console.error('Failed to load routes:', err);
        }
    };

    const handleCreateOperator = async (e) => {
        e.preventDefault();
        try {
            setLoading(true);
            const result = await governmentAPI.createOperator({
                cnic: operatorForm.cnic,
                fullName: operatorForm.fullName,
                phoneNumber: operatorForm.phoneNumber || null,
                routeID: operatorForm.routeID ? parseInt(operatorForm.routeID) : null,
                warehouseID: operatorForm.warehouseID ? parseInt(operatorForm.warehouseID) : null
            });

            if (result.success) {
                showMessage('✅ Operator created successfully!');
                setOperatorForm({ cnic: '', fullName: '', phoneNumber: '', routeID: '', warehouseID: '' });
                await loadOperators();
            } else {
                showMessage(result.message || 'Failed to create operator', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleDeactivateOperator = async (operatorID, name) => {
        if (!confirm(`Deactivate operator ${name}?`)) return;

        try {
            setLoading(true);
            const result = await governmentAPI.deactivateOperator(operatorID);

            if (result.success) {
                showMessage('✅ Operator deactivated successfully!');
                await loadOperators();
            } else {
                showMessage(result.message || 'Failed to deactivate operator', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleAssignRoute = async (operatorID, routeID, warehouseID) => {
        try {
            setLoading(true);
            const result = await governmentAPI.assignOperatorToRoute(
                operatorID.toString(),
                parseInt(routeID),
                parseInt(warehouseID)
            );

            if (result.success) {
                showMessage('✅ Route assigned successfully!');
                await loadOperators();
            } else {
                showMessage(result.message || 'Failed to assign route', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    // ================================
    // COMPLAINT FUNCTIONS
    // ================================

    const loadComplaints = async () => {
        try {
            const status = complaintFilter === 'all' ? null : complaintFilter;
            const data = await governmentAPI.getAllComplaints(status);
            setComplaints(data);
            console.log("loaded complaints: ", data);
        } catch (err) {
            console.error('Failed to load complaints:', err);
        }
    };

    useEffect(() => {
        if (activeTab === 'complaints') {
            loadComplaints();
        }
    }, [complaintFilter, activeTab]);

    const handleUpdateComplaintStatus = async (complaintID) => {
        const newStatus = 'Resolved';

        try {
            setLoading(true);
            const result = await governmentAPI.updateComplaintStatus(complaintID, newStatus);

            if (result.success) {
                showMessage('✅ Complaint status updated!');
                await loadComplaints();
            } else {
                showMessage(result.message || 'Failed to update status', 'error');
            }
        } catch (err) {
            showMessage('Error: ' + err.message, 'error');
        } finally {
            setLoading(false);
        }
    };

    // ================================
    // CHART DATA PREPARATION
    // ================================

    const getInventoryChartData = () => {
        return warehouseInventory.map(w => ({
            name: w.warehouseName,
            used: w.currentInventory,
            available: w.availableCapacity
        }));
    };

    const getTopAreasChartData = () => {
        return highYieldAreas.slice(0, 5).map(a => ({
            name: a.areaName,
            revenue: a.totalRevenue
        }));
    };

    const COLORS = ['#2E7D32', '#388E3C', '#43A047', '#4CAF50', '#66BB6A'];

    return (
        <div className="app-container">
            {/* Header */}
            <div className="header">
                <div>
                    <h1 className="header-title">🏛️ SmartWaste Government Portal</h1>
                    <p className="header-subtitle">Administrative Dashboard & Analytics</p>
                </div>
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
                <button className="btn btn-logout" onClick={onLogout}>Logout</button>
            </div>

            {/* Tabs */}
            <div className="tabs">
                {[
                    { id: 'warehouses', label: '🏭 Warehouses', count: warehouses.length },
                    { id: 'reports', label: '📊 Reports' },
                    { id: 'categories', label: '🗂️ Categories', count: categories.length },
                    { id: 'operators', label: '👷 Operators', count: operators.length },
                    { id: 'complaints', label: '📢 Complaints', count: complaints.length }
                ].map(tab => (
                    <button
                        key={tab.id}
                        className={`tab-button ${activeTab === tab.id ? 'active' : ''}`}
                        onClick={() => setActiveTab(tab.id)}
                    >
                        {tab.label} {tab.count !== undefined && `(${tab.count})`}
                    </button>
                ))}
            </div>

            {/* Message */}
            {message && (
                <div className={`message ${messageType === 'error' ? 'error' : 'success'}`}>
                    {message}
                </div>
            )}

            {/* Content */}
            <div className="content">
                {/* WAREHOUSES TAB */}
                {activeTab === 'warehouses' && (
                    <div>
                        <div className="section-header">
                            <h2>Warehouse Analytics</h2>
                            <div className="section-actions">
                                <select
                                    value={selectedWarehouse}
                                    onChange={(e) => setSelectedWarehouse(e.target.value)}
                                    className="select-input"
                                >
                                    <option value="">All Warehouses</option>
                                    {Array.isArray(warehouses) && warehouses.map(w => (
                                        <option key={w.warehouseID} value={w.warehouseID}>
                                            {w.warehouseName}
                                        </option>
                                    ))}
                                </select>
                                <button
                                    className="btn btn-green"
                                    onClick={() => loadWarehouseInventory(selectedWarehouse || null)}
                                >
                                    🔄 Refresh
                                </button>
                            </div>
                        </div>

                        {/* Inventory Charts */}
                        {warehouseInventory.length > 0 && (
                            <div className="grid-2">
                                <div className="card">
                                    <h3>Capacity Utilization</h3>
                                    <ResponsiveContainer width="100%" height={250}>
                                        <BarChart data={getInventoryChartData()}>
                                            <CartesianGrid strokeDasharray="3 3" />
                                            <XAxis dataKey="name" />
                                            <YAxis />
                                            <Tooltip />
                                            <Legend />
                                            <Bar dataKey="used" fill="#2E7D32" name="Used (kg)" />
                                            <Bar dataKey="available" fill="#A5D6A7" name="Available (kg)" />
                                        </BarChart>
                                    </ResponsiveContainer>
                                </div>

                                <div className="card">
                                    <h3>Capacity Distribution</h3>
                                    <ResponsiveContainer width="100%" height={250}>
                                        <PieChart>
                                            <Pie
                                                data={warehouseInventory.map(w => ({
                                                    name: w.warehouseName,
                                                    value: w.currentInventory
                                                }))}
                                                cx="50%"
                                                cy="50%"
                                                labelLine={false}
                                                label={(entry) => `${entry.name}: ${entry.value.toFixed(0)}kg`}
                                                outerRadius={80}
                                                fill="#8884d8"
                                                dataKey="value"
                                            >
                                                {warehouseInventory.map((entry, index) => (
                                                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                                ))}
                                            </Pie>
                                            <Tooltip />
                                        </PieChart>
                                    </ResponsiveContainer>
                                </div>
                            </div>
                        )}

                        {/* Inventory Table */}
                        <div className="card">
                            <table className="table">
                                <thead>
                                    <tr>
                                        <th>Warehouse</th>
                                        <th>Location</th>
                                        <th className="text-right">Capacity (kg)</th>
                                        <th className="text-right">Current (kg)</th>
                                        <th className="text-right">Used %</th>
                                        <th className="text-right">Categories</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {Array.isArray(warehouseInventory) && warehouseInventory.map(w => {
                                        const usedPercent = (w.capacityUsedPercent ?? 0) * 100;
                                        return (
                                            <tr key={w.warehouseID}>
                                                <td>{w.warehouseName}</td>
                                                <td>{w.areaName}, {w.city}</td>
                                                <td className="text-right">{(w.capacity ?? 0).toFixed(0)}</td>
                                                <td className="text-right">{(w.currentInventory ?? 0).toFixed(0)}</td>
                                                <td className="text-right">
                                                    <span className={`status-badge ${usedPercent > 80 ? 'danger' : usedPercent > 50 ? 'warning' : 'success'}`}>
                                                        {usedPercent.toFixed(1)}%
                                                    </span>
                                                </td>
                                                <td className="text-right">{w.categoryCount ?? 0}</td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div> *
                    </div>
                )}

                {/* REPORTS TAB */}
                {activeTab === 'reports' && (
                    <div>
                        <h2 className="section-title">Analytics & Reports</h2>

                        {/* High Yield Areas Chart */}
                        {Array.isArray(highYieldAreas) && (
                            <div className="card">
                                <h3>Top 5 High-Yield Areas</h3>
                                <ResponsiveContainer width="100%" height={300}>
                                    <BarChart data={getTopAreasChartData()}>
                                        <CartesianGrid strokeDasharray="3 3" />
                                        <XAxis dataKey="name" />
                                        <YAxis />
                                        <Tooltip />
                                        <Legend />
                                        <Bar dataKey="revenue" fill="#2E7D32" name="Revenue (Rs.)" />
                                    </BarChart>
                                </ResponsiveContainer>
                            </div>
                        )}

                        {/* High Yield Areas Table */}
                        <div className="card">
                            <h3>High-Yield Recycling Areas</h3>
                            <table className="table">
                                <thead>
                                    <tr>
                                        <th>Rank</th>
                                        <th>Area</th>
                                        <th>City</th>
                                        <th className="text-right">Listings</th>
                                        <th className="text-right">Weight (kg)</th>
                                        <th className="text-right">Revenue (Rs.)</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {Array.isArray(highYieldAreas) && highYieldAreas.map(area => (
                                        <tr key={area.areaID}>
                                            <td>
                                                <span className={`rank-badge ${area.revenueRank <= 3 ? 'top' : ''}`}>
                                                    {area.revenueRank}
                                                </span>
                                            </td>
                                            <td className="font-bold">{area.areaName}</td>
                                            <td>{area.city}</td>
                                            <td className="text-right">{area.totalListings}</td>
                                            <td className="text-right">{area.totalWeight.toFixed(2)}</td>
                                            <td className="text-right font-bold text-success">{area.totalRevenue.toFixed(2)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {/* Operator Performance */}
                        <div className="card">
                            <div className="section-header">
                                <h3>Operator Performance</h3>
                                <button className="btn btn-small btn-green" onClick={loadOperatorReport}>🔄 Refresh</button>
                            </div>

                            <table className="table">
                                <thead>
                                    <tr>
                                        <th>Operator</th>
                                        <th>Route</th>
                                        <th className="text-right">Total Waste (kg)</th>
                                        <th className="text-right">Trips</th>
                                        <th className="text-right">Efficiency Score</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {Array.isArray(operatorPerformance) && (
                                        <tr>
                                            <td colSpan="5" className="text-center text-muted">No performance data available</td>
                                        </tr>
                                    )}

                                    {Array.isArray(operatorPerformance) && operatorPerformance.map(op => (
                                        <tr key={op.operatorID}>
                                            <td>{op.fullName}</td>
                                            <td>{op.routeName || '—'}</td>
                                            <td className="text-right">{op.totalWasteCollected?.toFixed(0) || 0}</td>
                                            <td className="text-right">{op.totalTrips || 0}</td>
                                            <td className="text-right">
                                                <span className={`status-badge ${op.efficiencyScore >= 80 ? 'success' : op.efficiencyScore >= 50 ? 'warning' : 'danger'}`}>
                                                    {op.efficiencyScore?.toFixed(1) || 0}%
                                                </span>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}

                {activeTab === 'categories' && (
                    <div>
                        <h2 className="section-title">Waste Categories</h2>
                        <form onSubmit={handleCreateCategory} className="card form-card">
                            <input
                                type="text"
                                placeholder="Category Name"
                                value={categoryForm.categoryName}
                                onChange={e => setCategoryForm({ ...categoryForm, categoryName: e.target.value })}
                            />
                            <input
                                type="number"
                                placeholder="Base Price per Kg"
                                value={categoryForm.basePricePerKg}
                                onChange={e => setCategoryForm({ ...categoryForm, basePricePerKg: e.target.value })}
                            />
                            <input
                                type="text"
                                placeholder="Description"
                                value={categoryForm.description}
                                onChange={e => setCategoryForm({ ...categoryForm, description: e.target.value })}
                            />
                            <button className="btn btn-green">Create Category</button>
                        </form>

                        <div className="card">
                            <table className="table">
                                <thead>
                                    <tr>
                                        <th>Name</th>
                                        <th>Base Price</th>
                                        <th>Description</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {Array.isArray(categories) && categories.map(cat => (
                                        <tr key={cat.categoryID}>
                                            <td>{cat.categoryName}</td>
                                            <td>Rs. {cat.basePricePerKg}</td>
                                            <td>{cat.description || '-'}</td>
                                            <td>
                                                {editingCategory === cat.categoryID ? (
                                                    <>
                                                        <input
                                                            type="number"
                                                            value={newPrice}
                                                            onChange={(e) => setNewPrice(e.target.value)}
                                                            placeholder="Enter new price"
                                                            style={{ width: "120px", marginRight: "8px" }}
                                                        />

                                                        <button onClick={() => {
                                                            handleUpdatePrice(cat.categoryID, newPrice);
                                                            setEditingCategory(null);
                                                            setNewPrice("");
                                                        }}>
                                                            ✔ Save
                                                        </button>

                                                        <button onClick={() => {
                                                            setEditingCategory(null);
                                                            setNewPrice("");
                                                        }}>
                                                            ✖ Cancel
                                                        </button>
                                                    </>
                                                ) : (
                                                    <>
                                                        <button onClick={() => {
                                                            setEditingCategory(cat.categoryID);
                                                            setNewPrice(cat.basePricePerKg);
                                                        }}>
                                                            💰 Update Price
                                                        </button>
                                                        <button onClick={() => handleDeleteCategory(cat.categoryID, cat.categoryName)}>🗑️ Delete</button>
                                                    </>
                                                )}
                                            </td>

                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
                {activeTab === 'operators' && (
                    <div>
                        <h2 className="section-title">Operators Management</h2>

                        {/* Create Operator Form */}
                        <form onSubmit={handleCreateOperator} className="card form-card">
                            <input
                                type="text"
                                placeholder="Full Name"
                                value={operatorForm.fullName}
                                onChange={e => setOperatorForm({ ...operatorForm, fullName: e.target.value })}
                            />
                            <input
                                type="text"
                                placeholder="CNIC"
                                value={operatorForm.cnic}
                                onChange={e => setOperatorForm({ ...operatorForm, cnic: e.target.value })}
                            />
                            <input
                                type="text"
                                placeholder="Phone Number"
                                value={operatorForm.phoneNumber}
                                onChange={e => setOperatorForm({ ...operatorForm, phoneNumber: e.target.value })}
                            />
                            <input
                                type="number"
                                placeholder="Warehouse ID"
                                value={operatorForm.warehouseID}
                                onChange={e => setOperatorForm({ ...operatorForm, warehouseID: e.target.value })}
                            />
                            <input
                                type="number"
                                placeholder="Route ID (optional)"
                                value={operatorForm.routeID}
                                onChange={e => setOperatorForm({ ...operatorForm, routeID: e.target.value })}
                            />
                            <button className="btn btn-green">Create Operator</button>
                        </form>

                        {/* Operators Table */}
                        <div className="card">
                            <table className="table">
                                <thead>
                                    <tr>
                                        <th>Name</th>
                                        <th>CNIC</th>
                                        <th>Phone</th>
                                        <th>Route</th>
                                        <th>Warehouse</th>
                                        <th>Status</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {Array.isArray(operators) && operators.map(op => (
                                        <tr key={op.operatorID}>
                                            <td>{op.fullName}</td>
                                            <td>{op.cnic}</td>
                                            <td>{op.phoneNumber || '-'}</td>
                                            <td>{op.route?.routeName || '-'}</td>
                                            <td>{op.warehouse?.warehouseName || '-'}</td>
                                            <td>{op.status || '-'}</td>
                                            <td>
                                                {op.status !== "Offline" &&
                                                    <>
                                                        <select
                                                            className="route-dropdown"
                                                            defaultValue=""
                                                            onChange={(e) => {
                                                                const selectedRouteID = parseInt(e.target.value);
                                                                if (selectedRouteID) {
                                                                    handleAssignRoute(op.operatorID, selectedRouteID, op.warehouseID);
                                                                }
                                                            }}
                                                        >
                                                            <option value="" disabled>
                                                                Assign Route
                                                            </option>
                                                            {routes.map((route) => (
                                                                <option key={route.routeID} value={route.routeID}>
                                                                    {route.routeID}: {route.routeName}
                                                                </option>
                                                            ))}
                                                        </select>
                                                        <button
                                                            className="btn btn-small btn-red"
                                                            onClick={() => handleDeactivateOperator(op.operatorID, op.fullName)}
                                                        >
                                                            ⛔ Deactivate
                                                        </button>
                                                    </>
                                                }
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}

                {/* Complaints Table with Nested Info */}
                {activeTab == "complaints" && <div className="card">
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Complaint ID</th>
                                <th>Type</th>
                                <th>Description</th>
                                <th>Date</th>
                                <th className="text-right">Status</th>
                                <th className="text-right">Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {Array.isArray(complaints) && complaints.map(c => {

                                return (
                                    <React.Fragment key={c.complaintID}>
                                        <tr>
                                            <td>{c.complaintID}</td>
                                            <td>{c.complaintType}</td>
                                            <td>{c.description}</td>
                                            <td>{new Date(c.createdAt).toLocaleString()}</td>
                                            <td className="text-right">
                                                <span className={`status-badge ${c.status === 'Resolved' ? 'success' : 'danger'}`}>
                                                    {c.status}
                                                </span>
                                            </td>
                                            <td className="text-right">
                                                {c.status !== 'Resolved' && (
                                                    <button
                                                        className="btn btn-small btn-green"
                                                        onClick={() => handleUpdateComplaintStatus(c.complaintID)}
                                                    >
                                                        ✅ Mark as Resolved
                                                    </button>
                                                )}
                                                <button
                                                    className="btn btn-small btn-blue ml-2"
                                                    onClick={() => toggleExpanded(c.complaintID)}
                                                >
                                                    {expandedComplaints[c.complaintID] ? "🔽 Hide Details" : "🔍 View Details"}
                                                </button>
                                            </td>
                                        </tr>

                                        {/* Nested Info */}
                                        {expandedComplaints[c.complaintID] && (
                                            <tr>
                                                <td colSpan={6} className="nested-info">
                                                    <strong>Citizen Info:</strong>
                                                    <div>Full Name: {c.citizen?.fullName}</div>
                                                    <div>Address: {c.citizen?.address}</div>
                                                    <div>Phone: {c.citizen?.phoneNumber}</div>

                                                    <strong>Operator Info:</strong>
                                                    <div>Full Name: {c.operator?.fullName}</div>
                                                    <div>Phone: {c.operator?.phoneNumber}</div>
                                                    <div>Status: {c.operator?.status}</div>

                                                    <strong>Route & Warehouse:</strong>
                                                    <div>Route ID: {c.routeID ?? "N/A"}</div>
                                                    <div>Warehouse ID: {c.warehouseID ?? "N/A"}</div>
                                                </td>
                                            </tr>
                                        )}
                                    </React.Fragment>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
                }



            </div>
        </div >
    );
}