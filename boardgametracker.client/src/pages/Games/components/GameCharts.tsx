import { ScoringRankChart } from './charts/ScoringRankChart';
import { PlaysByWeekDayChart } from './charts/PlaysByWeekDayChart';
import { PlayerScoringChart } from './charts/PlayerScoringChart';
import { PlayerCountChart } from './charts/PlayerCountChart';

export const GameCharts = () => {
  return (
    <div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-3 gap-3">
      <div className="col-span-1">
        <ScoringRankChart />
      </div>
      <div className="col-span-1">
        <PlayerCountChart />
      </div>
      <div className="col-span-1">
        <PlayerScoringChart />
      </div>
    </div>
  );
};
