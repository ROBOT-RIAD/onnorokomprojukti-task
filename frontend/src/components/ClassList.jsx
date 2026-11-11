import React from 'react';
import { Users, CheckCircle2, Clock, Check, AlertCircle } from 'lucide-react';

export default function ClassList({
  classes,
  isLoading,
  error,
  studentId,
  onApply,
  isApplying,
  activeClassApplyingId,
}) {
  const isValidStudent = studentId && /^\d{17}$/.test(studentId);

  if (isLoading) {
    return (
      <div className="card loading-card">
        <div className="spinner"></div>
        <p>ক্লাসের তথ্য লোড হচ্ছে...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger">
        <AlertCircle size={20} />
        <div>
          <strong>তথ্য লোড করতে সমস্যা হয়েছে</strong>
          <p>{error.message || 'সার্ভার থেকে ক্লাসের তথ্য পাওয়া যায়নি।'}</p>
        </div>
      </div>
    );
  }

  if (!classes || classes.length === 0) {
    return (
      <div className="card empty-card">
        <Users size={48} className="empty-icon" />
        <h3>এখনও কোনো ক্লাস যোগ করা হয়নি</h3>
        <p>নতুন ক্লাস যুক্ত করতে উপরের "নতুন ক্লাস তৈরি করুন" বোতামে ক্লিক করুন।</p>
      </div>
    );
  }

  return (
    <div className="class-grid">
      {classes.map((cls) => {
        const isFull = cls.availableSeats <= 0;
        const isThisApplying = isApplying && activeClassApplyingId === cls.id;

        return (
          <div key={cls.id} className={`class-card ${isFull ? 'full-class' : ''}`}>
            <div className="class-card-header">
              <h3 className="class-name">{cls.name}</h3>
              <span className={`seat-badge ${isFull ? 'seat-badge-full' : 'seat-badge-available'}`}>
                {isFull ? 'আসন পূর্ণ' : `${cls.availableSeats} টি আসন খালি`}
              </span>
            </div>

            <div className="seats-overview">
              <div className="seat-stat">
                <span className="stat-label">মোট আসন</span>
                <span className="stat-value">{cls.totalSeats}</span>
              </div>
              <div className="seat-stat">
                <span className="stat-label">সংরক্ষিত (Held)</span>
                <span className="stat-value text-warning">{cls.heldSeats}</span>
              </div>
              <div className="seat-stat">
                <span className="stat-label">নিশ্চিত (Confirmed)</span>
                <span className="stat-value text-success">{cls.confirmedSeats}</span>
              </div>
              <div className="seat-stat">
                <span className="stat-label">খালি (Available)</span>
                <span className="stat-value text-primary"><strong>{cls.availableSeats}</strong></span>
              </div>
            </div>

            <div className="seat-progress-bar">
              <div
                className="progress-fill progress-confirmed"
                style={{ width: `${(cls.confirmedSeats / cls.totalSeats) * 100}%` }}
                title={`নিশ্চিত: ${cls.confirmedSeats}`}
              />
              <div
                className="progress-fill progress-held"
                style={{ width: `${(cls.heldSeats / cls.totalSeats) * 100}%` }}
                title={`সংরক্ষিত: ${cls.heldSeats}`}
              />
            </div>

            <div className="class-card-footer">
              <button
                type="button"
                onClick={() => onApply(cls.id)}
                disabled={isFull || isThisApplying || !isValidStudent}
                className={`btn btn-block ${isFull ? 'btn-disabled' : 'btn-primary'}`}
                title={!isValidStudent ? 'আগে উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর দিন' : (isFull ? 'কোনো আসন খালি নেই' : 'সিট সংরক্ষণ করুন')}
              >
                {isThisApplying ? (
                  <>
                    <span className="spinner-sm"></span>
                    আবেদন প্রসেস হচ্ছে...
                  </>
                ) : isFull ? (
                  'আসন খালি নেই'
                ) : (
                  'আবেদন করুন'
                )}
              </button>

              {!isValidStudent && (
                <p className="helper-hint">
                  * আবেদন করতে উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর সেট করুন
                </p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
