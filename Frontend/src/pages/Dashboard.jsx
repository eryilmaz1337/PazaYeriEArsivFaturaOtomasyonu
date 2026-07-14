import React, { useEffect, useState } from 'react';
import axios from 'axios';

const Dashboard = () => {
    const [stats, setStats] = useState({
        totalOrders: 0,
        invoicedOrders: 0,
        pendingInvoices: 0,
        failedInvoices: 0,
        totalRevenue: 0
    });

    useEffect(() => {
        axios.get('http://localhost:5000/api/dashboard/stats')
            .then(res => setStats(res.data))
            .catch(err => console.error("Stats API Hatası", err));
    }, []);

    return (
        <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-8">Pazar Yeri Fatura Durumu</h1>
            
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                
                <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
                    <div className="flex items-center">
                        <div className="flex-shrink-0 bg-indigo-50 rounded-md p-3">
                            <svg className="h-6 w-6 text-indigo-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
                            </svg>
                        </div>
                        <div className="ml-5 w-0 flex-1">
                            <dl>
                                <dt className="text-sm font-medium text-gray-500 truncate">Toplam Sipariş</dt>
                                <dd className="text-2xl font-semibold text-gray-900">{stats.totalOrders}</dd>
                            </dl>
                        </div>
                    </div>
                </div>

                <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
                    <div className="flex items-center">
                        <div className="flex-shrink-0 bg-green-50 rounded-md p-3">
                            <svg className="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                        </div>
                        <div className="ml-5 w-0 flex-1">
                            <dl>
                                <dt className="text-sm font-medium text-gray-500 truncate">Kesilen Fatura</dt>
                                <dd className="text-2xl font-semibold text-gray-900">{stats.invoicedOrders}</dd>
                            </dl>
                        </div>
                    </div>
                </div>

                <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
                    <div className="flex items-center">
                        <div className="flex-shrink-0 bg-yellow-50 rounded-md p-3">
                            <svg className="h-6 w-6 text-yellow-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                        </div>
                        <div className="ml-5 w-0 flex-1">
                            <dl>
                                <dt className="text-sm font-medium text-gray-500 truncate">Bekleyen Fatura</dt>
                                <dd className="text-2xl font-semibold text-gray-900">{stats.pendingInvoices}</dd>
                            </dl>
                        </div>
                    </div>
                </div>

                <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
                    <div className="flex items-center">
                        <div className="flex-shrink-0 bg-red-50 rounded-md p-3">
                            <svg className="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                        </div>
                        <div className="ml-5 w-0 flex-1">
                            <dl>
                                <dt className="text-sm font-medium text-gray-500 truncate">Hatalı İşlemler</dt>
                                <dd className="text-2xl font-semibold text-gray-900">{stats.failedInvoices}</dd>
                            </dl>
                        </div>
                    </div>
                </div>

            </div>
            
            <div className="mt-8 bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                <h3 className="text-lg leading-6 font-medium text-gray-900">Toplam Hacim</h3>
                <div className="mt-2 text-4xl font-extrabold text-indigo-600">
                    ₺{stats.totalRevenue.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                </div>
                <p className="mt-1 text-sm text-gray-500">Sisteme kaydedilen siparişlerin toplam ciro değeri</p>
            </div>
        </div>
    );
};

export default Dashboard;
