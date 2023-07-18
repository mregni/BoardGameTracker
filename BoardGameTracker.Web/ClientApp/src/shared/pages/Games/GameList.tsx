import {Col, ConfigProvider, Layout, Row} from 'antd';
import {useContext} from 'react';

import {GcCard} from '../../components/GcCard';
import {GcNoDataLoader} from '../../components/GcNoDataLoader';
import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {GamesContext} from './context';

const GameList = () => {
  const { games, loading } = useContext(GamesContext);

  return (
    <GcPageContainer>
      <GcPageContainerHeader 
      title='Games'
      isLoading={loading || games.length === 0}>
        Add, filter, dfg
      </GcPageContainerHeader>
      <GcPageContainerContent isLoading={loading || games.length === 0}>
          <Row gutter={[10, 10]}>
            {games.map(game =>
              <Col xxl={2} xl={4} md={6} sm={12} xs={12} key={game.id}>
                <GcCard
                  id={game.id}
                  title={game.title}
                  image={game.image}
                  detailPage="games"
                  state={game.state}
                  showPlayLink />
              </Col>
            )}
          </Row>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

export default GameList