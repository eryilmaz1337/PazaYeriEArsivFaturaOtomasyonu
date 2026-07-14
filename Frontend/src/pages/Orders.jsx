import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import axios from 'axios';

const platformConfig = {
    Trendyol: {
        title: 'Trendyol Siparişleri',
        bg: 'bg-orange-100',
        text: 'text-orange-800',
        border: 'border-orange-200',
        accentBg: 'bg-orange-500',
        accentHover: 'hover:bg-orange-600',
        headerBg: 'bg-orange-50',
        icon: (
            <svg className="w-3.5 h-3.5 mr-1" fill="currentColor" viewBox="0 0 20 20">
                <path d="M10 2a8 8 0 100 16 8 8 0 000-16zM8.5 7.5a1.5 1.5 0 113 0 1.5 1.5 0 01-3 0zM10 13a3 3 0 01-2.83-2h5.66A3 3 0 0110 13z"/>
            </svg>
        ),
    },
    Hepsiburada: {
        title: 'Hepsiburada Siparişleri',
        bg: 'bg-purple-100',
        text: 'text-purple-800',
        border: 'border-purple-200',
        accentBg: 'bg-purple-600',
        accentHover: 'hover:bg-purple-700',
        headerBg: 'bg-purple-50',
        icon: (
            <svg className="w-3.5 h-3.5 mr-1" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 2a4 4 0 00-4 4v1H5a1 1 0 00-.994.89l-1 9A1 1 0 004 18h12a1 1 0 00.994-1.11l-1-9A1 1 0 0015 7h-1V6a4 4 0 00-4-4zm2 5V6a2 2 0 10-4 0v1h4zm-6 3a1 1 0 112 0 1 1 0 01-2 0zm7-1a1 1 0 100 2 1 1 0 000-2z" clipRule="evenodd"/>
            </svg>
        ),
    },
};

const defaultConfig = {
    title: 'Tüm Siparişler',
    bg: 'bg-indigo-100',
    text: 'text-indigo-800',
    border: 'border-indigo-200',
    accentBg: 'bg-indigo-600',
    accentHover: 'hover:bg-indigo-700',
    headerBg: 'bg-gray-50',
    icon: null,
};

const getStyle = (platform) => platformConfig[platform] || defaultConfig;

const Orders = () => {
    const { platform } = useParams(); // URL'den platform parametresini al
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [processingId, setProcessingId] = useState(null);

    // Platform adını büyük harfle düzelt (url'de küçük harf olacak)
    const platformName = platform
        ? platform.charAt(0).toUpperCase() + platform.slice(1).toLowerCase()
        : null;

    const config = platformName ? (platformConfig[platformName] || defaultConfig) : defaultConfig;

    const fetchOrders = () => {
        setLoading(true);
        axios.get('http://localhost:5000/api/orders')
            .then(res => {
                let data = res.data;
                // Platform filtresi varsa uygula
                if (platformName) {
                    data = data.filter(o => o.platform === platformName);
                }
                setOrders(data);
                setLoading(false);
            })
            .catch(err => {
                console.error("Orders API Hatası", err);
                setLoading(false);
            });
    };

    useEffect(() => {
        fetchOrders();
    }, [platform]); // platform değiştiğinde yeniden çek

    const handleKesFatura = async (id) => {
        if (!window.confirm("Bu sipariş için e-arşiv fatura kesilecek. Onaylıyor musunuz?")) return;

        setProcessingId(id);
        try {
            const res = await axios.post(`http://localhost:5000/api/orders/${id}/invoice`);
            if (res.data.success) {
                alert("Fatura başarıyla kesildi!");
                fetchOrders();
            }
        } catch (err) {
            console.error("Fatura kesim hatası:", err);
            alert("Fatura kesilirken bir hata oluştu: " + (err.response?.data?.message || err.message));
        } finally {
            setProcessingId(null);
        }
    };

    if (loading) return <div className="text-center mt-20 text-indigo-600 font-medium">Yükleniyor...</div>;

    return (
        <div className="flex flex-col">
            {/* Sayfa başlığı - platforma göre renkli */}
            <div className="flex items-center mb-6">
                {platformName && platformConfig[platformName]?.icon && (
                    <span className={`inline-flex items-center justify-center w-10 h-10 rounded-lg mr-3 ${getStyle(platformName).bg}`}>
                        <span className={`${getStyle(platformName).text} scale-150`}>
                            {platformConfig[platformName].icon}
                        </span>
                    </span>
                )}
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">{config.title}</h1>
                    <p className="text-sm text-gray-500 mt-1">
                        {orders.length} sipariş bulundu
                    </p>
                </div>
            </div>

            <div className="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
                <div className="py-2 align-middle inline-block min-w-full sm:px-6 lg:px-8">
                    <div className="shadow overflow-hidden border-b border-gray-200 sm:rounded-lg">
                        <table className="min-w-full divide-y divide-gray-200">
                            <thead className={config.headerBg}>
                                <tr>
                                    {/* Platform kolonu sadece "Tümü" sayfasında göster */}
                                    {!platformName && (
                                        <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                            Platform
                                        </th>
                                    )}
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Sipariş No
                                    </th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Ürün Adı
                                    </th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Müşteri
                                    </th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Tutar
                                    </th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Fatura Durumu
                                    </th>
                                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        İşlem
                                    </th>
                                </tr>
                            </thead>
                            <tbody className="bg-white divide-y divide-gray-200">
                                {orders.map((order) => {
                                    const style = getStyle(order.platform);
                                    return (
                                        <tr key={order.id} className="hover:bg-gray-50 transition-colors duration-150">
                                            {/* Platform badge - sadece "Tümü" sayfasında */}
                                            {!platformName && (
                                                <td className="px-6 py-4 whitespace-nowrap">
                                                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold border ${style.bg} ${style.text} ${style.border}`}>
                                                        {style.icon}
                                                        {order.platform}
                                                    </span>
                                                </td>
                                            )}
                                            <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                                {order.orderNumber}
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 truncate max-w-xs" title={order.productName}>
                                                {order.productName || 'Bilinmiyor'}
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm text-gray-900">{order.customerName}</div>
                                                <div className="text-xs text-gray-400">{new Date(order.orderDate).toLocaleDateString('tr-TR')}</div>
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                                ₺{Number(order.totalAmount).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap">
                                                {order.isInvoiced ? (
                                                    <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">
                                                        Kesildi
                                                    </span>
                                                ) : order.invoiceDetails && !order.invoiceDetails.isSuccess ? (
                                                     <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800 cursor-help" title={order.invoiceDetails.errorMessage}>
                                                        Hatalı (Üzerine Gel)
                                                    </span>
                                                ) : (
                                                    <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-yellow-100 text-yellow-800">
                                                        Bekliyor
                                                    </span>
                                                )}
                                            </td>
                                            <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                                                {order.isInvoiced ? (
                                                    <span className="text-green-600 font-semibold flex items-center">
                                                        <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20"><path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd"></path></svg>
                                                        Onaylı
                                                    </span>
                                                ) : (
                                                    <button 
                                                        onClick={() => handleKesFatura(order.id)}
                                                        disabled={processingId === order.id}
                                                        className={`px-3 py-1 rounded-md text-sm font-medium text-white transition-colors duration-200 ${
                                                            processingId === order.id 
                                                            ? 'bg-gray-400 cursor-not-allowed' 
                                                            : `${config.accentBg} ${config.accentHover} shadow-sm`
                                                        }`}
                                                    >
                                                        {processingId === order.id ? 'İşleniyor...' : (order.invoiceDetails && !order.invoiceDetails.isSuccess ? 'Tekrar Dene' : 'Fatura Kes')}
                                                    </button>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                        {orders.length === 0 && (
                            <div className="text-center py-10 text-gray-500">
                                {platformName
                                    ? `${platformName} platformundan hiç sipariş bulunamadı.`
                                    : 'Hiç sipariş bulunamadı.'}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Orders;
