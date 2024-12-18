import { useLocation } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { cx } from 'class-variance-authority';
import { Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';

import { BgtMenuLogo } from '../BgtMenu/BgtMenuLogo';
import { BgtMenuItem } from '../BgtMenu/BgtMenuItem';
import { useMenuItems } from '../../hooks/useMenuItems';

import { useBgtMenuBar } from './hooks/useBgtMenuBar';

const MobileMenu = () => {
  const [open, setOpen] = useState(false);
  const { menuItems } = useMenuItems();
  const { counts } = useBgtMenuBar();
  const location = useLocation();

  useEffect(() => {
    setOpen(false);
  }, [location]);

  useEffect(() => {
    const closeMenu = () => setOpen(false);
    window.addEventListener('scroll', closeMenu);

    return () => {
      window.removeEventListener('scroll', closeMenu);
    };
  }, []);

  if (counts.data === undefined) return null;

  return (
    <div className="flex-col flex justify-between md:hidden">
      <div className=" bg-gray-950 z-50">
        <div className="px-4 pt-2 grow flex flex-row justify-between items-center">
          <BgtMenuLogo />
          {!open && <Bars3Icon height={25} className="pr-3" onClick={() => setOpen(true)} />}
          {open && <XMarkIcon height={25} className="pr-3" onClick={() => setOpen(false)} />}
        </div>
      </div>
      <div className={cx('mobile-menu bg-gray-950 absolute w-full top-16 z-40', !open && 'hidden-menu')}>
        {menuItems.map((x) => (
          <BgtMenuItem key={x.path} item={x} count={counts.data.find((y) => y.key == x.path)?.value} />
        ))}
      </div>
    </div>
  );
};

const BgtMenuBar = () => {
  const { counts } = useBgtMenuBar();
  const { menuItems } = useMenuItems();

  if (counts.data === undefined) return null;

  return (
    <>
      <div className={cx('hidden relative md:flex bg-card-black h-full flex-col justify-between w-64')}>
        <div className="px-4 flex flex-col">
          <BgtMenuLogo />
          <div className="mt-4">
            {menuItems.map((x) => (
              <BgtMenuItem key={x.path} item={x} count={counts.data.find((y) => y.key == x.path)?.value} />
            ))}
          </div>
        </div>
      </div>
      <MobileMenu />
    </>
  );
};

export default BgtMenuBar;
