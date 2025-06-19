import { useNavigate } from 'react-router-dom';
import { t } from 'i18next';

import { RoundDecimal } from '../../utils/numberUtils';
import { useSettings } from '../../hooks/useSettings';
import { BgtTextStatistic } from '../../components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtMostWinnerCard } from '../../components/BgtCard/BgtMostWinnerCard';

import { useDashboardPage } from './hooks/useDashboardPage';
import { GameStateChart } from './components/GameStateChart';

export const DashboardPage = () => {
  const { statistics, charts } = useDashboardPage();
  const { settings } = useSettings();
  const navigate = useNavigate();

  if (statistics.data === undefined || charts.data === undefined || settings === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.dashboard')} actions={[]} />
      <BgtPageContent>
        <h1>TODO</h1>
        <ul>
          <li>✅ New game (manual)</li>
          <li>✅ Edit game</li>
          <li>✅ Delete game</li>
          <li>✅ Edit location</li>
          <li>✅ Delete location</li>
          <li>✅ New location</li>
          <li>✅ Edit player</li>
          <li>✅ User session list</li>
          <li>✅ Delete player</li>
          <li>✅ Move icons to svg files</li>
          <li>✅ View session table</li>
          <li>✅ Edit session</li>
          <li>✅ Delete session</li>
          <li>✅ Use decimal seperator from settings</li>
          <li>✅ Load language from settings after settings save (if updated)</li>
          <li>✅ Add github button for feature requests</li>
          <li>✅ Add crowdin button for translations</li>
          <li>✅ Remove "info for nerds" page</li>
          <li>✅ Add version number in environment settings</li>
          <li>✅ Disable "add player" button in modal when creating a player</li>
          <li>Add loaders everywhere</li>
          <li>✅ Add docker build for win and linux (like embystat)</li>
          <li>Filter games on state</li>
          <li>Filter games on tags</li>
          <li>Make tags clickable in game detail and navigate to game overview with filter</li>
          <li>
            User achievements:
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Played 5, 10, 50, 100 sessions
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- DONE - Played 3, 10, 20, 50 different games
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Won 3, 10, 25, 50 sessions
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Played for 5, 10, 50, 100 hours
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Have a win percentage of 30%, 50%, 70%, 90% (with minimum of 5 sessions
            played)
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Solo Specialist: Win 5, 10, 25, 50 solo games
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Win 5, 10, 15, 25 games in a row
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Social Player: Play with 5, 10, 25, 50 different people
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Won with 2 or less points difference from second place
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Almost there!: Lost with 2 or less points difference from first place
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Marathon Runner: Play a single game session lasting 4+ hours
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- First Try: Win a game on your first play
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Learning Curve: Beat your personal best score 3 times in the same game
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Monthly Goal: Play 20+ games in a single month
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Consistent Schedule: Play every Saturday for 10 weeks straight
          </li>
          <li>REMOVE ME WHEN ALL IS DONE</li>
        </ul>
        <h1>Bugs</h1>
        <ul>
          <li>✅ EditDropdown menu component sluit niet bij wegklikken ergens op het scherm</li>
          <li>✅ Toasts verdwijnen achter de posters op de gsm. Misschien andere toast gebruiken?</li>
          <li>✅ Fix scroll in long pages (background is shorter then page)</li>
          <li>
            Bij aanmaken van locatie word bij een tweede create de lijst niet meer geupdate na de POST, ook de counts
            query word niet refreshed
          </li>
          <li>✅ Player met score 0 kan niet aan sessie worden toegevoegd.</li>
          <li>✅ Fix layout on create player modal (photo should be left to name)</li>
          <li>
            ✅ BgtImageCard has a link to deletGame and a delete modal. This should not happen or add a "delete"
            function
          </li>
        </ul>
        <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
          <BgtTextStatistic content={statistics.data.gameCount} title={t('statistics.game-count')} />
          <BgtTextStatistic content={statistics.data.playerCount} title={t('statistics.player-count')} />
          <BgtTextStatistic content={statistics.data.sessionCount} title={t('statistics.session-count')} />
          <BgtTextStatistic content={statistics.data.locationCount} title={t('statistics.location-count')} />
          <BgtTextStatistic
            content={statistics.data.totalCost}
            prefix={settings.data?.currency}
            title={t('statistics.total-cost')}
          />
          <BgtTextStatistic
            content={RoundDecimal(statistics.data.meanPayed, 0.5)}
            prefix={settings.data?.currency}
            title={t('statistics.mean-cost')}
          />

          <BgtTextStatistic
            content={statistics.data.totalPlayTime}
            title={t('statistics.total-playtime')}
            suffix={t('common.minutes-abbreviation')}
          />
          <BgtTextStatistic
            content={RoundDecimal(statistics.data.meanPlayTime, 1)}
            title={t('statistics.mean-playtime')}
            suffix={t('common.minutes-abbreviation')}
          />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-1 md:gap-3">
          <div>
            <BgtMostWinnerCard
              image={statistics.data.mostWinningPlayer?.image}
              name={statistics.data.mostWinningPlayer?.name}
              value={statistics.data.mostWinningPlayer?.totalWins}
              onClick={() => navigate(`/players/${statistics.data.mostWinningPlayer?.id}`)}
              nameHeader={t('statistics.most-wins')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <div>
            <GameStateChart charts={charts.data} />
          </div>
        </div>
      </BgtPageContent>
    </BgtPage>
  );
};
