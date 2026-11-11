import { useState, useEffect } from 'react';

export function useCountdown(expiresAtStr, onExpire) {
  const calculateRemaining = () => {
    if (!expiresAtStr) return 0;
    const target = new Date(expiresAtStr).getTime();
    const now = new Date().getTime();
    return Math.max(0, Math.floor((target - now) / 1000));
  };

  const [remaining, setRemaining] = useState(calculateRemaining);

  useEffect(() => {
    setRemaining(calculateRemaining());

    const interval = setInterval(() => {
      const nextRemaining = calculateRemaining();
      setRemaining(nextRemaining);

      if (nextRemaining <= 0) {
        clearInterval(interval);
        if (onExpire) {
          onExpire();
        }
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [expiresAtStr]);

  const minutes = Math.floor(remaining / 60);
  const seconds = remaining % 60;
  const formatted = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  const isExpired = remaining <= 0;

  return { remaining, formatted, isExpired };
}
