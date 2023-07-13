import {Col, Row} from 'antd';
import {useContext} from 'react';

import {GcCard} from '../../components/GcCard';
import {GcNoDataLoader} from '../../components/GcNoDataLoader';
import {GamesContext} from './context';

const GameList = () => {
  const { games, loading } = useContext(GamesContext);

  return (
    <GcNoDataLoader isLoading={loading || games.length === 0}>
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
    </GcNoDataLoader>
  )
}

export default GameList