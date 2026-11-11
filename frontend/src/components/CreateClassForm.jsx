import React, { useState } from 'react';
import { PlusCircle, School, Hash, Sparkles } from 'lucide-react';

export default function CreateClassForm({ onCreateClass, isCreating }) {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState('120');
  const [error, setError] = useState('');

  const presets = [
    { name: 'Class 6 – Morning Shift', seats: 120 },
    { name: 'Class 6 – Day Shift', seats: 120 },
    { name: 'Class 7 – Morning Shift', seats: 100 },
    { name: 'Class 9 – Science Morning', seats: 80 },
  ];

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('ক্লাসের নাম আবশ্যক');
      return;
    }
    const seats = parseInt(totalSeats, 10);
    if (isNaN(seats) || seats <= 0) {
      setError('আসন সংখ্যা ১ বা তার বেশি হতে হবে');
      return;
    }

    setError('');
    onCreateClass({
      name: name.trim(),
      totalSeats: seats,
    });
  };

  const handleApplyPreset = (preset) => {
    setName(preset.name);
    setTotalSeats(String(preset.seats));
    setError('');
  };

  return (
    <div className="card admin-card">
      <div className="card-header">
        <div>
          <h3 className="card-title">নতুন ক্লাস তৈরি করুন (School Admin)</h3>
          <p className="card-subtitle">প্রতিটি শিফট আলাদা একটি ক্লাস হিসেবে তৈরি হবে</p>
        </div>
        <span className="badge badge-admin">এডমিন প্যানেল</span>
      </div>

      <div className="card-body">
        <div className="presets-bar">
          <span className="presets-title"><Sparkles size={14} /> দ্রুত প্রিসেট:</span>
          {presets.map((p, i) => (
            <button
              key={i}
              type="button"
              className="btn btn-preset"
              onClick={() => handleApplyPreset(p)}
            >
              {p.name} ({p.seats})
            </button>
          ))}
        </div>

        <form onSubmit={handleSubmit} className="admin-form">
          <div className="form-row">
            <div className="form-group flex-2">
              <label htmlFor="className" className="form-label">
                <School size={15} /> ক্লাসের নাম ও শিফট
              </label>
              <input
                id="className"
                type="text"
                placeholder="যেমন: Class 6 – Morning Shift"
                value={name}
                onChange={(e) => {
                  setName(e.target.value);
                  if (error) setError('');
                }}
                className="form-control"
              />
            </div>

            <div className="form-group flex-1">
              <label htmlFor="totalSeats" className="form-label">
                <Hash size={15} /> মোট আসন সংখ্যা
              </label>
              <input
                id="totalSeats"
                type="number"
                min="1"
                placeholder="120"
                value={totalSeats}
                onChange={(e) => {
                  setTotalSeats(e.target.value);
                  if (error) setError('');
                }}
                className="form-control"
              />
            </div>

            <div className="form-group form-action">
              <button
                type="submit"
                disabled={isCreating}
                className="btn btn-primary btn-create"
              >
                <PlusCircle size={18} />
                {isCreating ? 'তৈরি হচ্ছে...' : 'ক্লাস তৈরি করুন'}
              </button>
            </div>
          </div>

          {error && <p className="form-error">{error}</p>}
        </form>
      </div>
    </div>
  );
}
