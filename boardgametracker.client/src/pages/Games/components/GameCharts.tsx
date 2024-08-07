import { ScoringRankChart } from './charts/ScoringRankChart';
import { PlayerScoringChart } from './charts/PlayerScoringChart';
import { PlayerCountChart } from './charts/PlayerCountChart';

export const GameCharts = () => {
  return (
    <div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-3 gap-3">
      <ScoringRankChart />
      <PlayerCountChart />
      {/* <PlayerScoringChart /> */}
    </div>
  );
};
