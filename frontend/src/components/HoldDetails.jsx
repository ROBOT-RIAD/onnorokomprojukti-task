import React, { useState } from 'react';
import { useCountdown } from './useCountdown';
import { Clock, CheckCircle, XCircle, AlertTriangle, CreditCard, Ban } from 'lucide-react';

export default function HoldDetails({ hold, studentId, onConfirm, onCancel, isConfirming, isCancelling }) {
  const [localExpired, setLocalExpired] = useState(false);

  const { formatted, isExpired } = useCountdown(hold?.expiresAt, () => {
    setLocalExpired(true);
  });

  if (!hold) return null;

  const currentStatus = (hold.status === 'HELD' && (isExpired || localExpired)) ? 'EXPIRED' : hold.status;
  const canPay = currentStatus === 'HELD' && !isExpired && !localExpired;
  const canCancel = currentStatus === 'HELD' && !isExpired && !localExpired;

  const getStatusBadge = (status) => {
    switch (status) {
      case 'CONFIRMED':
        return (
          <span className="badge badge-success">
            <CheckCircle size={15} /> ভর্তি নিশ্চিত (CONFIRMED)
          </span>
        );
      case 'HELD':
        return (
          <span className="badge badge-warning">
            <Clock size={15} /> আসন সংরক্ষিত (HELD)
          </span>
        );
      case 'EXPIRED':
        return (
          <span className="badge badge-danger">
            <AlertTriangle size={15} /> মেয়াদোত্তীর্ণ (EXPIRED)
          </span>
        );
      case 'CANCELLED':
        return (
          <span className="badge badge-secondary">
            <XCircle size={15} /> বাতিল (CANCELLED)
          </span>
        );
      default:
        return <span className="badge">{status}</span>;
    }
  };

  return (
    <div className="card hold-card">
      <div className="card-header">
        <div>
          <h3 className="card-title">আবেদনের তথ্য</h3>
          <p className="card-subtitle">আবেদন আইডি: <code>{hold.holdId}</code></p>
        </div>
        <div>
          {getStatusBadge(currentStatus)}
        </div>
      </div>

      <div className="card-body">
        <div className="hold-grid">
          {hold.className && (
            <div className="info-item">
              <span className="info-label">ক্লাস ও শিফট</span>
              <span className="info-value"><strong>{hold.className}</strong></span>
            </div>
          )}

          <div className="info-item">
            <span className="info-label">ক্লাস আইডি</span>
            <span className="info-value"><code>{hold.classId}</code></span>
          </div>

          <div className="info-item">
            <span className="info-label">শিক্ষার্থী জন্ম নিবন্ধন নম্বর</span>
            <span className="info-value"><code>{hold.studentId || studentId}</code></span>
          </div>

          {currentStatus === 'HELD' && (
            <div className="info-item countdown-box">
              <span className="info-label">মেয়াদ শেষ হতে কত সময় বাকি</span>
              <span className="countdown-timer">
                <Clock size={18} className="timer-icon" />
                <strong className={isExpired ? 'expired-text' : 'active-timer'}>{formatted}</strong>
              </span>
            </div>
          )}
        </div>

        {currentStatus === 'CONFIRMED' && (
          <div className="alert alert-success">
            <CheckCircle size={20} />
            <div>
              <strong>অভিনন্দন! ভর্তি ফি সফলভাবে জমা হয়েছে।</strong>
              <p>শিক্ষার্থীর ভর্তি নিশ্চিত করা হয়েছে। কোনো অতিরিক্ত পদক্ষেপ প্রয়োজন নেই।</p>
            </div>
          </div>
        )}

        {currentStatus === 'EXPIRED' && (
          <div className="alert alert-danger">
            <AlertTriangle size={20} />
            <div>
              <strong>আবেদনের মেয়াদ শেষ হয়ে গেছে!</strong>
              <p>নির্ধারিত সময়ের মধ্যে ফি জমা না দেওয়ায় আসনটি অন্যদের জন্য উন্মুক্ত করা হয়েছে।</p>
            </div>
          </div>
        )}

        {currentStatus === 'CANCELLED' && (
          <div className="alert alert-secondary">
            <XCircle size={20} />
            <div>
              <strong>আবেদনটি বাতিল করা হয়েছে।</strong>
              <p>আসনটি পুনরায় খালি তালিকায় যুক্ত হয়েছে।</p>
            </div>
          </div>
        )}

        <div className="hold-actions">
          <button
            type="button"
            onClick={() => onConfirm(hold.holdId)}
            disabled={!canPay || isConfirming}
            className="btn btn-success btn-lg"
          >
            <CreditCard size={18} />
            {isConfirming ? 'ফি জমা হচ্ছে...' : 'ভর্তি ফি দিন'}
          </button>

          <button
            type="button"
            onClick={() => onCancel(hold.holdId)}
            disabled={!canCancel || isCancelling}
            className="btn btn-outline-danger"
          >
            <Ban size={18} />
            {isCancelling ? 'বাতিল হচ্ছে...' : 'বাতিল'}
          </button>
        </div>
      </div>
    </div>
  );
}
