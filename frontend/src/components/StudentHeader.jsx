import React, { useState, useEffect } from 'react';
import { User, CheckCircle2, AlertCircle } from 'lucide-react';

export default function StudentHeader({ studentId, onStudentIdChange }) {
  const [inputValue, setInputValue] = useState(studentId || '');
  const [error, setError] = useState('');

  useEffect(() => {
    setInputValue(studentId || '');
  }, [studentId]);

  const isValid17 = /^\d{17}$/.test(inputValue.trim());

  const handleSave = (e) => {
    e?.preventDefault();
    const val = inputValue.trim();
    if (!val) {
      setError('জন্ম নিবন্ধন নম্বর লিখুন');
      return;
    }
    if (!/^\d{17}$/.test(val)) {
      setError('জন্ম নিবন্ধন নম্বরটি ঠিক ১৭ অঙ্কের সংখ্যা হতে হবে');
      return;
    }
    setError('');
    onStudentIdChange(val);
  };

  const handleClear = () => {
    setInputValue('');
    setError('');
    onStudentIdChange('');
  };

  return (
    <header className="header-card">
      <div className="header-brand">
        <div className="brand-logo">🏫</div>
        <div>
          <h1 className="brand-title">School Admission System</h1>
          <p className="brand-subtitle">অনলাইন ভর্তি কার্যক্রম ও আবেদন পোর্টাল</p>
        </div>
      </div>

      <div className="student-box">
        <form onSubmit={handleSave} className="student-form">
          <div className="student-input-group">
            <span className="input-icon">
              <User size={18} />
            </span>
            <input
              type="text"
              maxLength={17}
              placeholder="শিক্ষার্থীর ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর"
              value={inputValue}
              onChange={(e) => {
                const numeric = e.target.value.replace(/\D/g, '');
                setInputValue(numeric);
                if (error) setError('');
              }}
              className={`student-input ${isValid17 ? 'valid' : ''}`}
            />
            <span className="char-count">{inputValue.length}/17</span>
          </div>

          <button
            type="submit"
            disabled={!isValid17}
            className="btn btn-primary"
          >
            পরিচয় সেট করুন
          </button>

          {studentId && (
            <button
              type="button"
              onClick={handleClear}
              className="btn btn-secondary"
            >
              মুছুন
            </button>
          )}
        </form>

        {error && (
          <div className="inline-error">
            <AlertCircle size={14} />
            <span>{error}</span>
          </div>
        )}

        {studentId && isValid17 && (
          <div className="active-student-pill">
            <CheckCircle2 size={15} color="#16a34a" />
            <span>সক্রিয় শিক্ষার্থী আইডি: <strong>{studentId}</strong></span>
          </div>
        )}
      </div>
    </header>
  );
}
