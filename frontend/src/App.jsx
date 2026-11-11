import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getClasses,
  createClass,
  createHold,
  getHoldStatus,
  confirmHold,
  cancelHold,
  getMyHolds,
} from './api';
import StudentHeader from './components/StudentHeader';
import CreateClassForm from './components/CreateClassForm';
import ClassList from './components/ClassList';
import HoldDetails from './components/HoldDetails';
import MyApplications from './components/MyApplications';
import {
  BookOpen,
  FileCheck2,
  Settings,
  AlertCircle,
  CheckCircle2,
  X,
} from 'lucide-react';

export default function App() {
  const queryClient = useQueryClient();

  // Load student ID from localStorage for persistence across browser refresh/reopen
  const [studentId, setStudentId] = useState(() => {
    return localStorage.getItem('school_admission_student_id') || '20152691234567890';
  });

  const [activeTab, setActiveTab] = useState('classes'); // 'classes' | 'my-applications' | 'admin'
  const [currentHoldId, setCurrentHoldId] = useState(() => {
    return localStorage.getItem('school_admission_last_hold_id') || null;
  });
  const [applyingClassId, setApplyingClassId] = useState(null);
  const [notification, setNotification] = useState(null);

  // Sync student ID with localStorage
  const handleStudentIdChange = (newId) => {
    setStudentId(newId);
    if (newId) {
      localStorage.setItem('school_admission_student_id', newId);
    } else {
      localStorage.removeItem('school_admission_student_id');
      setCurrentHoldId(null);
      localStorage.removeItem('school_admission_last_hold_id');
    }
  };

  const showToast = (message, type = 'info') => {
    setNotification({ message, type });
  };

  // 1. Query for Classes - auto refreshes every 3 seconds without page reload
  const {
    data: classes = [],
    isLoading: isClassesLoading,
    error: classesError,
  } = useQuery({
    queryKey: ['classes'],
    queryFn: getClasses,
    refetchInterval: 3000,
    refetchIntervalInBackground: true,
  });

  // 2. Query for My Applications - auto refreshes every 5 seconds
  const {
    data: myApplications = [],
    isLoading: isMyAppsLoading,
    error: myAppsError,
  } = useQuery({
    queryKey: ['myHolds', studentId],
    queryFn: () => getMyHolds(studentId),
    enabled: Boolean(studentId && /^\d{17}$/.test(studentId)),
    refetchInterval: 5000,
    refetchIntervalInBackground: true,
  });

  // 3. Query for active hold details if currentHoldId is set
  const {
    data: currentHold,
    refetch: refetchCurrentHold,
  } = useQuery({
    queryKey: ['hold', currentHoldId],
    queryFn: async () => {
      if (!currentHoldId) return null;
      const hold = await getHoldStatus(currentHoldId);
      // augment with class name if available
      const foundCls = classes.find((c) => c.id === hold.classId);
      return {
        ...hold,
        className: foundCls ? foundCls.name : '',
        studentId,
      };
    },
    enabled: Boolean(currentHoldId),
    refetchInterval: 4000,
  });

  // Mutation: Create Class (Admin)
  const createClassMutation = useMutation({
    mutationFn: createClass,
    onSuccess: (newClass) => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      showToast(`নতুন ক্লাস "${newClass.name}" সফলভাবে তৈরি হয়েছে!`, 'success');
      setActiveTab('classes');
    },
    onError: (err) => {
      showToast(err.message || 'ক্লাস তৈরি করতে সমস্যা হয়েছে', 'error');
    },
  });

  // Mutation: Create Hold / Apply
  const createHoldMutation = useMutation({
    mutationFn: ({ classId, studentId }) => createHold(classId, studentId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      queryClient.invalidateQueries({ queryKey: ['myHolds', studentId] });
      setCurrentHoldId(data.holdId);
      localStorage.setItem('school_admission_last_hold_id', data.holdId);
      setApplyingClassId(null);
      showToast('আসন সফলভাবে সংরক্ষিত (Hold) হয়েছে! অনুগ্রহ করে নির্ধারিত সময়ের মধ্যে ফি প্রদান করুন।', 'success');
      // Scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    onError: (err) => {
      setApplyingClassId(null);
      let msg = err.message;
      if (err.statusCode === 409) {
        msg = 'আসন খালি নেই অথবা আপনি ইতিমধ্যেই এই ক্লাসে আবেদন করেছেন।';
      }
      showToast(msg, 'error');
    },
  });

  // Mutation: Confirm Hold
  const confirmHoldMutation = useMutation({
    mutationFn: ({ holdId, studentId }) => confirmHold(holdId, studentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      queryClient.invalidateQueries({ queryKey: ['myHolds', studentId] });
      queryClient.invalidateQueries({ queryKey: ['hold', currentHoldId] });
      showToast('ভর্তি ফি সফলভাবে জমা হয়েছে এবং ভর্তি নিশ্চিত করা হয়েছে!', 'success');
    },
    onError: (err) => {
      showToast(err.message || 'ভর্তি ফি জমা করতে সমস্যা হয়েছে', 'error');
    },
  });

  // Mutation: Cancel Hold
  const cancelHoldMutation = useMutation({
    mutationFn: ({ holdId, studentId }) => cancelHold(holdId, studentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      queryClient.invalidateQueries({ queryKey: ['myHolds', studentId] });
      queryClient.invalidateQueries({ queryKey: ['hold', currentHoldId] });
      showToast('আবেদন সফলভাবে বাতিল করা হয়েছে।', 'info');
    },
    onError: (err) => {
      showToast(err.message || 'আবেদন বাতিল করতে সমস্যা হয়েছে', 'error');
    },
  });

  const handleApply = (classId) => {
    if (!studentId || !/^\d{17}$/.test(studentId)) {
      showToast('আবেদন করতে প্রথমে উপরে আপনার ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর দিন।', 'error');
      return;
    }
    setApplyingClassId(classId);
    createHoldMutation.mutate({ classId, studentId });
  };

  const handleConfirm = (holdId) => {
    confirmHoldMutation.mutate({ holdId, studentId });
  };

  const handleCancel = (holdId) => {
    if (window.confirm('আপনি কি নিশ্চিতভাবে এই আসন সংরক্ষণ বাতিল করতে চান?')) {
      cancelHoldMutation.mutate({ holdId, studentId });
    }
  };

  return (
    <div className="app-container">
      {/* Student Identity Header */}
      <StudentHeader
        studentId={studentId}
        onStudentIdChange={handleStudentIdChange}
      />

      {/* Global Toast Notification */}
      {notification && (
        <div className={`toast toast-${notification.type}`}>
          <div className="toast-content">
            {notification.type === 'success' && <CheckCircle2 size={18} />}
            {notification.type === 'error' && <AlertCircle size={18} />}
            {notification.type === 'info' && <BookOpen size={18} />}
            <span>{notification.message}</span>
          </div>
          <button
            type="button"
            className="toast-close"
            onClick={() => setNotification(null)}
          >
            <X size={16} />
          </button>
        </div>
      )}

      {/* Current Active Hold Banner / Details */}
      {currentHold && currentHold.status && (
        <section className="current-hold-section">
          <HoldDetails
            hold={currentHold}
            studentId={studentId}
            onConfirm={handleConfirm}
            onCancel={handleCancel}
            isConfirming={confirmHoldMutation.isPending}
            isCancelling={cancelHoldMutation.isPending}
          />
        </section>
      )}

      {/* Navigation Tabs */}
      <nav className="tab-nav">
        <button
          type="button"
          className={`tab-btn ${activeTab === 'classes' ? 'active' : ''}`}
          onClick={() => setActiveTab('classes')}
        >
          <BookOpen size={18} />
          <span>ক্লাস তালিকা ও আবেদন</span>
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'my-applications' ? 'active' : ''}`}
          onClick={() => setActiveTab('my-applications')}
        >
          <FileCheck2 size={18} />
          <span>আমার আবেদন</span>
          {myApplications.length > 0 && (
            <span className="tab-counter">{myApplications.length}</span>
          )}
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'admin' ? 'active' : ''}`}
          onClick={() => setActiveTab('admin')}
        >
          <Settings size={18} />
          <span>স্কুল এডমিন (ক্লাস তৈরি)</span>
        </button>
      </nav>

      {/* Main Content Area */}
      <main className="main-content">
        {activeTab === 'classes' && (
          <section>
            <div className="section-header">
              <h2 className="section-title">উপলব্ধ ক্লাস ও আসন অবস্থা</h2>
              <p className="section-subtitle">
                "আগে আবেদন করলে আগে পাবেন" নীতি প্রযোজ্য। আসন প্রতি ৩ সেকেন্ড পর পর স্বয়ংক্রিয়ভাবে আপডেট হয়।
              </p>
            </div>

            <ClassList
              classes={classes}
              isLoading={isClassesLoading}
              error={classesError}
              studentId={studentId}
              onApply={handleApply}
              isApplying={createHoldMutation.isPending}
              activeClassApplyingId={applyingClassId}
            />
          </section>
        )}

        {activeTab === 'my-applications' && (
          <section>
            <MyApplications
              applications={myApplications}
              isLoading={isMyAppsLoading}
              error={myAppsError}
              studentId={studentId}
              onConfirm={handleConfirm}
              onCancel={handleCancel}
              isConfirming={confirmHoldMutation.isPending}
              isCancelling={cancelHoldMutation.isPending}
            />
          </section>
        )}

        {activeTab === 'admin' && (
          <section>
            <CreateClassForm
              onCreateClass={(data) => createClassMutation.mutate(data)}
              isCreating={createClassMutation.isPending}
            />
          </section>
        )}
      </main>

      <footer className="footer">
        <p>School Admission System &bull; Assistant Software Engineer Take-home Assignment</p>
        <p className="footer-sub">.NET 8+ Backend &bull; React (Vite + TanStack Query) &bull; PostgreSQL &bull; Docker</p>
      </footer>
    </div>
  );
}
