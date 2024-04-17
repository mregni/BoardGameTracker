import {PlayerCountChart} from './charts/PlayerCountChart';
import {PlayerScoringChart} from './charts/PlayerScoringChart';
import {PlaysByWeekDayChart} from './charts/PlaysByWeekDayChart';
import {ScoringRankChart} from './charts/ScoringRankChart';

export const GameCharts = () => {
  return (
    <>
      <div className='col-span-1'>
        <PlaysByWeekDayChart />
      </div>
      <div className='col-span-1'>
        <PlayerCountChart />
      </div>
      <div className='col-span-1'>
        <PlayerScoringChart />
      </div>
      <div className='col-span-1'>
        <ScoringRankChart />
      </div>
    </>
  )
}