import React, { useState } from 'react';
import { useCountdown } from './useCountdown';
import { FileText, Clock, CheckCircle2, AlertTriangle, XCircle, CreditCard, Ban } from 'lucide-react';

function ApplicationItem({ app, studentId, onConfirm, onCancel, isConfirming, isCancelling }) {
  const [localExpired, setLocalExpired] = useState(false);

  const { formatted, isExpired } = useCountdown(app.expiresAt, () => {
    setLocalExpired(true);
  });

  const currentStatus = (app.status === 'HELD' && (isExpired || localExpired)) ? 'EXPIRED' : app.status;
  const canPay = currentStatus === 'HELD' && !isExpired && !localExpired;
  const canCancel = currentStatus === 'HELD' && !isExpired && !localExpired;

  const renderBadge = (status) => {
    switch (status) {
      case 'CONFIRMED':
        return <span className="badge badge-success"><CheckCircle2 size={14} /> ভর্তি নিশ্চিত</span>;
      case 'HELD':
        return <span className="badge badge-warning"><Clock size={14} /> আসন সংরক্ষিত ({formatted})</span>;
      case 'EXPIRED':
        return <span className="badge badge-danger"><AlertTriangle size={14} /> মেয়াদোত্তীর্ণ</span>;
      case 'CANCELLED':
        return <span className="badge badge-secondary"><XCircle size={14} /> বাতিল</span>;
      default:
        return <span className="badge">{status}</span>;
    }
  };

  return (
    <div className="my-app-card">
      <div className="my-app-header">
        <div>
          <h4 className="my-app-class-name">{app.className || `ক্লাস আইডি: ${app.classId}`}</h4>
          <span className="my-app-id">আবেদন আইডি: <code>{app.holdId}</code></span>
        </div>
        <div>
          {renderBadge(currentStatus)}
        </div>
      </div>

      <div className="my-app-body">
        {currentStatus === 'HELD' && (
          <div className="my-app-countdown">
            <Clock size={16} />
            <span>ফি জমা দেওয়ার সময় বাকি: <strong>{formatted}</strong></span>
          </div>
        )}

        <div className="my-app-actions">
          {currentStatus === 'HELD' && (
            <>
              <button
                type="button"
                onClick={() => onConfirm(app.holdId)}
                disabled={!canPay || isConfirming}
                className="btn btn-sm btn-success"
              >
                <CreditCard size={14} />
                {isConfirming ? 'জমা হচ্ছে...' : 'ভর্তি ফি দিন'}
              </button>

              <button
                type="button"
                onClick={() => onCancel(app.holdId)}
                disabled={!canCancel || isCancelling}
                className="btn btn-sm btn-outline-danger"
              >
                <Ban size={14} />
                {isCancelling ? 'বাতিল হচ্ছে...' : 'বাতিল'}
              </button>
            </>
          )}

          {currentStatus === 'CONFIRMED' && (
            <span className="text-success small-text">
              ✓ ভর্তি ফি গৃহীত ও আসন নিশ্চিত হয়েছে
            </span>
          )}

          {currentStatus === 'EXPIRED' && (
            <span className="text-danger small-text">
              ✕ সময় শেষ হওয়ায় আসন বাতিল হয়েছে
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

export default function MyApplications({
  applications,
  isLoading,
  error,
  studentId,
  onConfirm,
  onCancel,
  isConfirming,
  isCancelling,
}) {
  if (!studentId || !/^\d{17}$/.test(studentId)) {
    return (
      <div className="card empty-card">
        <FileText size={48} className="empty-icon" />
        <h3>শিক্ষার্থী পরিচয় প্রদান করুন</h3>
        <p>নিজের আবেদনসমূহ দেখতে উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর সেট করুন।</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="card loading-card">
        <div className="spinner"></div>
        <p>আবেদনের তথ্য লোড হচ্ছে...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger">
        <AlertTriangle size={20} />
        <div>
          <strong>আবেদনের তথ্য আনতে সমস্যা হয়েছে</strong>
          <p>{error.message || 'সার্ভার থেকে তথ্য পাওয়া যায়নি।'}</p>
        </div>
      </div>
    );
  }

  if (!applications || applications.length === 0) {
    return (
      <div className="card empty-card">
        <FileText size={48} className="empty-icon" />
        <h3>কোনো আবেদন পাওয়া যায়নি</h3>
        <p>এই শিক্ষার্থী আইডির অধীনে কোনো আসন সংরক্ষিত বা নিশ্চিত করা হয়নি। ক্লাস তালিকা থেকে আবেদন করুন।</p>
      </div>
    );
  }

  return (
    <div className="my-apps-container">
      <div className="section-header">
        <h3 className="section-title">আমার সকল আবেদন ({applications.length})</h3>
        <p className="section-subtitle">শিক্ষার্থী আইডি: <code>{studentId}</code></p>
      </div>

      <div className="my-apps-list">
        {applications.map((app) => (
          <ApplicationItem
            key={app.holdId}
            app={app}
            studentId={studentId}
            onConfirm={onConfirm}
            onCancel={onCancel}
            isConfirming={isConfirming}
            isCancelling={isCancelling}
          />
        ))}
      </div>
    </div>
  );
}
