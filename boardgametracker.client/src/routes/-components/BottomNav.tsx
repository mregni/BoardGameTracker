import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { cva, cx } from 'class-variance-authority';
import { useNavigate, useRouterState } from '@tanstack/react-router';

import { BgtText } from '@/components/BgtText/BgtText';
import { BgtMenuItem } from '@/components/BgtMenu/BgtMenuItem';
import { useBgtMenuBar } from '@/components/BgtLayout/hooks/useBgtMenuBar';
import More from '@/assets/icons/more.svg?react';

export const BottomNav = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { versionInfo, menuItems, counts } = useBgtMenuBar();
  const [showMoreMenu, setShowMoreMenu] = useState(false);
  const routerState = useRouterState();
  const currentPath = routerState.location.pathname;

  const handleMoreClick = () => {
    setShowMoreMenu(!showMoreMenu);
  };

  const handleMenuItemClick = (path: string) => {
    if (path === 'more') {
      handleMoreClick();
    } else {
      navigate({ to: path });
    }
  };

  const isMoreMenuActive = menuItems.some((x) => !x.mobileVisible && x.path === currentPath);

  const mobileMenuItems = menuItems.filter((x) => x.mobileVisible);
  mobileMenuItems.push({ icon: More, menuLabel: 'common.more', path: 'more', mobileVisible: true });

  return (
    <>
      {showMoreMenu && (
        <div className="fixed inset-0 bg-black/50 z-30 md:hidden" onClick={() => setShowMoreMenu(false)} />
      )}

      {showMoreMenu && (
        <div className="fixed bottom-20 w-full z-40 md:hidden border border-white/10 rounded-t-2xl overflow-hidden shadow-2xl bg-background/95">
          <div className="px-4 py-3 border-b border-white/10 bg-background font-mono flex justify-between">
            <BgtText color="white" opacity={40}>
              {t('version.version')}
            </BgtText>
            <BgtText color="white" opacity={40}>
              {versionInfo?.currentVersion}
            </BgtText>
          </div>

          <div>
            {menuItems
              .filter((x) => !x.mobileVisible)
              .map((item) => (
                <BgtMenuItem
                  onClick={() => setShowMoreMenu(false)}
                  key={item.path}
                  item={item}
                  count={counts?.find((y) => item.path.endsWith(y.key))?.value}
                  className="m-0!"
                />
              ))}
          </div>
        </div>
      )}

      <nav className="md:hidden fixed bottom-0 left-0 right-0 bg-background/95 backdrop-blur-lg border-t border-white/10 z-40 safe-area-inset-bottom">
        <div className="flex items-center justify-between">
          {mobileMenuItems.map((item) => {
            const isActive = item.path === 'more' ? isMoreMenuActive || showMoreMenu : currentPath === item.path;

            return (
              <button
                key={item.path}
                onClick={() => handleMenuItemClick(item.path)}
                className="relative flex flex-col items-center justify-center gap-1 py-2 flex-1 h-20 transition-all active:scale-95"
              >
                {isActive && (
                  <div className="absolute top-0 left-1/2 -translate-x-1/2 w-12 h-1 bg-primary rounded-full" />
                )}

                <div className={cx('relative transition-all', isActive && 'scale-110')}>
                  {isActive && <div className="absolute inset-0 bg-primary/20 blur-lg rounded-full" />}
                  <item.icon
                    className={cx(
                      'relative transition-colors size-6',
                      isActive ? 'text-primary stroke-[2.5]' : 'text-gray-400 stroke-2'
                    )}
                  />
                </div>
                <BgtText size="1" color={isActive ? 'primary' : 'gray'} className={cx('text-center transition-colors')}>
                  {t(item.menuLabel)}
                </BgtText>
              </button>
            );
          })}
        </div>
      </nav>
    </>
  );
};
