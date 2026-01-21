import { VersionCard } from './VersionCard';

import { BgtMenuLogo } from '@/components/BgtMenu/BgtMenuLogo';
import { BgtMenuItem } from '@/components/BgtMenu/BgtMenuItem';
import { useBgtMenuBar } from '@/components/BgtLayout/hooks/useBgtMenuBar';

export const Sidebar = () => {
  const { counts, versionInfo, menuItems } = useBgtMenuBar();

  if (counts === undefined) return null;

  return (
    <div className="hidden md:block">
      <aside className="w-64 bg-background h-full border-r border-white/10 flex flex-col">
        <div className="p-6">
          <BgtMenuLogo />
        </div>

        <nav className="flex-1 px-3">
          {menuItems.map((x) => (
            <BgtMenuItem key={x.path} item={x} count={counts.find((y) => x.path.endsWith(y.key))?.value} />
          ))}
        </nav>

        <VersionCard versionInfo={versionInfo} />
      </aside>
    </div>
  );
};
