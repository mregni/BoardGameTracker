import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';

import { BgtText } from '@/components/BgtText/BgtText';

export type SettingsCategory = 'general' | 'shelf-of-shame' | 'game-nights' | 'advanced';

interface CategoryItem {
  id: SettingsCategory;
  label: string;
  description: string;
}

const CATEGORIES: CategoryItem[] = [
  { id: 'general', label: 'settings.sidebar.general.title', description: 'settings.sidebar.general.description' },
  {
    id: 'shelf-of-shame',
    label: 'settings.sidebar.shelf-of-shame.title',
    description: 'settings.sidebar.shelf-of-shame.description',
  },
  {
    id: 'game-nights',
    label: 'settings.sidebar.game-nights.title',
    description: 'settings.sidebar.game-nights.description',
  },
  { id: 'advanced', label: 'settings.sidebar.advanced.title', description: 'settings.sidebar.advanced.description' },
];

interface Props {
  activeCategory: SettingsCategory;
  onCategoryChange: (category: SettingsCategory) => void;
}

export const SettingsSidebar = ({ activeCategory, onCategoryChange }: Props) => {
  const { t } = useTranslation();

  return (
    <div className="w-full lg:w-64 border-b lg:border-b-0 lg:border-r border-white/10 lg:pr-4 max-lg:pb-4">
      <nav className="space-y-1">
        {CATEGORIES.map((category) => {
          const isActive = activeCategory === category.id;

          return (
            <button
              key={category.id}
              onClick={() => onCategoryChange(category.id)}
              className={cx(
                'w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-all text-left cursor-pointer',
                isActive
                  ? 'bg-primary/20 border border-primary/50 text-white'
                  : 'text-white/70 hover:bg-white/5 border border-transparent hover:text-white'
              )}
            >
              <div className="flex-1 min-w-0">
                <BgtText size="2" weight="medium" color="white">
                  {t(category.label)}
                </BgtText>
                <BgtText size="1" color="white" opacity={50} className="truncate">
                  {t(category.description)}
                </BgtText>
              </div>
            </button>
          );
        })}
      </nav>
    </div>
  );
};
