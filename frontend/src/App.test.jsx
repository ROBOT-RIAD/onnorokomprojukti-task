import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import StudentHeader from './components/StudentHeader';
import ClassList from './components/ClassList';
import HoldDetails from './components/HoldDetails';

describe('Frontend Component Tests', () => {
  it('StudentHeader accepts only numeric digits and allows setting 17-digit student id', () => {
    const onStudentIdChange = vi.fn();
    render(<StudentHeader studentId="" onStudentIdChange={onStudentIdChange} />);

    const input = screen.getByPlaceholderText('শিক্ষার্থীর ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর');
    const submitBtn = screen.getByRole('button', { name: /পরিচয় সেট করুন/i });

    // Button disabled when empty or less than 17 digits
    expect(submitBtn.disabled).toBe(true);

    // Type 17 digits
    fireEvent.change(input, { target: { value: '20152691234567890' } });
    expect(submitBtn.disabled).toBe(false);

    // Click submit
    fireEvent.click(submitBtn);
    expect(onStudentIdChange).toHaveBeenCalledWith('20152691234567890');
  });

  it('ClassList displays class seats and disables Apply when seats are full', () => {
    const classes = [
      {
        id: 'c_1',
        name: 'Class 6 - Morning Shift',
        totalSeats: 120,
        heldSeats: 10,
        confirmedSeats: 10,
        availableSeats: 100,
      },
      {
        id: 'c_2',
        name: 'Class 6 - Day Shift',
        totalSeats: 50,
        heldSeats: 25,
        confirmedSeats: 25,
        availableSeats: 0,
      },
    ];

    render(
      <ClassList
        classes={classes}
        isLoading={false}
        error={null}
        studentId="20152691234567890"
        onApply={() => {}}
        isApplying={false}
      />
    );

    expect(screen.getByText('Class 6 - Morning Shift')).toBeDefined();
    expect(screen.getByText('100 টি আসন খালি')).toBeDefined();
    expect(screen.getByText('Class 6 - Day Shift')).toBeDefined();
    expect(screen.getByText('আসন পূর্ণ')).toBeDefined();

    // The full class should have disabled button
    const fullButton = screen.getByRole('button', { name: /আসন খালি নেই/i });
    expect(fullButton.disabled).toBe(true);
  });

  it('HoldDetails disables payment button when hold is expired', () => {
    const expiredHold = {
      holdId: 'h_test_1',
      classId: 'c_1',
      className: 'Class 6 - Morning Shift',
      status: 'EXPIRED',
      expiresAt: new Date(Date.now() - 60000).toISOString(),
    };

    render(
      <HoldDetails
        hold={expiredHold}
        studentId="20152691234567890"
        onConfirm={() => {}}
        onCancel={() => {}}
        isConfirming={false}
        isCancelling={false}
      />
    );

    expect(screen.getByText(/মেয়াদোত্তীর্ণ/i)).toBeDefined();
    const payBtn = screen.getByRole('button', { name: /ভর্তি ফি দিন/i });
    expect(payBtn.disabled).toBe(true);
  });
});
