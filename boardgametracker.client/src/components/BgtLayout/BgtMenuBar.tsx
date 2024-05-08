import { clsx } from 'clsx';
import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';

import { Bars3Icon, ChevronDoubleLeftIcon, ChevronDoubleRightIcon, XMarkIcon } from '@heroicons/react/24/outline';

import { useCounts } from '../../hooks/useCounts';
import { useMenuItems } from '../../hooks/useMenuItems';
import { BgtBottomButton } from '../BgtMenu/BgtBottomButton';
import { BgtMenuItem } from '../BgtMenu/BgtMenuItem';
import { BgtMenuLogo } from '../BgtMenu/BgtMenuLogo';

const MobileMenu = () => {
  const [open, setOpen] = useState(false);
  const { menuItems } = useMenuItems();
  const { counts } = useCounts();
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

  if (!counts) return null;

  return (
    <div className="flex-col flex justify-between md:hidden">
      <div className=" bg-gray-950 z-50">
        <div className="px-4 pt-2 grow flex flex-row justify-between items-center">
          <BgtMenuLogo />
          {!open && <Bars3Icon height={25} className="pr-3" onClick={() => setOpen(true)} />}
          {open && <XMarkIcon height={25} className="pr-3" onClick={() => setOpen(false)} />}
        </div>
      </div>
      <div className={clsx('mobile-menu bg-gray-950 absolute w-full top-16 z-40', !open && 'hidden-menu')}>
        {menuItems.map((x) => (
          <BgtMenuItem fullSize={true} key={x.path} item={x} count={counts.find((y) => y.key == x.path)?.value} />
        ))}
      </div>
    </div>
  );
};

const BgtMenuBar = () => {
  const { counts } = useCounts();
  const { menuItems } = useMenuItems();
  const [fullSizeMenu, setFullSizeMenu] = useState<boolean>(true);

  if (!counts) return null;

  return (
    <>
      <div className={clsx('hidden relative md:flex bg-gray-950 h-full flex-col justify-between ', !fullSizeMenu && 'w-20', fullSizeMenu && 'w-64')}>
        <div className="px-4 flex flex-col">
          <BgtMenuLogo fullSize={fullSizeMenu} />
          <div className="mt-6">
            {menuItems.map((x) => (
              <BgtMenuItem fullSize={fullSizeMenu} key={x.path} item={x} count={counts.find((y) => y.key == x.path)?.value} />
            ))}
          </div>
        </div>
        <div className={clsx('flex justify-center place-content-center bg-sky-900', { 'flex-row': fullSizeMenu }, { 'flex-col': !fullSizeMenu })}>
          {fullSizeMenu && <BgtBottomButton icon={<ChevronDoubleLeftIcon />} onClick={() => setFullSizeMenu(false)} />}
          {!fullSizeMenu && <BgtBottomButton icon={<ChevronDoubleRightIcon />} onClick={() => setFullSizeMenu(true)} />}
        </div>
      </div>
      <MobileMenu />
    </>
  );
};

export default BgtMenuBar;
