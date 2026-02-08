import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';

export type FilterType = 'all' | 'upcoming' | 'past';

interface Props {
  filter: FilterType;
  onFilterChange: (filter: FilterType) => void;
  allCount: number;
  upcomingCount: number;
  pastCount: number;
}

export const FilterTabs = (props: Props) => {
  const { filter, onFilterChange, allCount, upcomingCount, pastCount } = props;
  const { t } = useTranslation();

  const tabs: { key: FilterType; label: string; count: number }[] = [
    { key: 'all', label: t('common.all'), count: allCount },
    { key: 'upcoming', label: t('game-nights.upcoming'), count: upcomingCount },
    { key: 'past', label: t('game-nights.past'), count: pastCount },
  ];

  return (
    <div className="flex gap-2">
      {tabs.map((tab) => (
        <button
          key={tab.key}
          onClick={() => onFilterChange(tab.key)}
          className={cx(
            'px-4 py-2 rounded-lg transition-colors',
            filter === tab.key ? 'bg-primary text-white' : 'bg-white/5 text-gray-400 hover:bg-white/10'
          )}
        >
          {tab.label} ({tab.count})
        </button>
      ))}
    </div>
  );
};
