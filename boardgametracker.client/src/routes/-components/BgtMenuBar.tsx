import { useTranslation } from 'react-i18next';
import { useEffect, useState } from 'react';
import { cx } from 'class-variance-authority';
import { useLocation } from '@tanstack/react-router';

import { BgtMenuLogo } from '../../components/BgtMenu/BgtMenuLogo';
import { BgtMenuItem } from '../../components/BgtMenu/BgtMenuItem';
import { useBgtMenuBar } from '../../components/BgtLayout/hooks/useBgtMenuBar';

import XIcon from '@/assets/icons/x.svg?react';
import List from '@/assets/icons/list.svg?react';

const MobileMenu = () => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const location = useLocation();
  const { environment, menuItems, counts } = useBgtMenuBar();

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

  if (counts === undefined) return null;

  return (
    <div className="flex-col flex justify-between md:hidden">
      <div className=" bg-gray-950 z-50">
        <div className="px-4 pt-2 grow flex flex-row justify-between items-center">
          <BgtMenuLogo />
          {!open && <List height={25} className="pr-3" onClick={() => setOpen(true)} />}
          {open && <XIcon height={25} className="pr-3" onClick={() => setOpen(false)} />}
        </div>
      </div>
      <div className={cx('mobile-menu bg-gray-950 absolute w-full top-14 z-40', !open && 'hidden-menu')}>
        {menuItems.map((x) => (
          <BgtMenuItem key={x.path} item={x} count={counts.find((y) => x.path.endsWith(y.key))?.value} />
        ))}
        <div>
          {environment && (
            <div className="flex justify-center items-center h-9 text-gray-500 text-sm">
              {t('settings.environment.version')}: {environment.version}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

const BgtMenuBar = () => {
  const { t } = useTranslation();
  const { counts, environment, settings, menuItems } = useBgtMenuBar();

  if (counts === undefined) return null;

  return (
    <div>
      <div className="hidden relative md:flex bg-card-black md:h-full flex-col justify-between w-64">
        <div className="px-4 flex flex-col">
          <BgtMenuLogo />
          <div className="mt-4">
            {menuItems.map((x) => (
              <BgtMenuItem key={x.path} item={x} count={counts.find((y) => x.path.endsWith(y.key))?.value} />
            ))}
          </div>
        </div>
        {environment && (
          <div className="flex justify-center items-center h-16 text-gray-500">
            {t('settings.environment.version')}: {environment.version}
          </div>
        )}
      </div>
      <MobileMenu settings={settings} />
    </div>
  );
};

export default BgtMenuBar;
