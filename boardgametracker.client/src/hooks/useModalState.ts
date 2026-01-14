import { useState, useCallback } from 'react';

export const useModalState = (initialState = false) => {
  const [isOpen, setIsOpen] = useState(initialState);

  const show = useCallback(() => setIsOpen(true), []);
  const hide = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen((prev) => !prev), []);

  return { isOpen, show, hide, toggle, setIsOpen };
};
