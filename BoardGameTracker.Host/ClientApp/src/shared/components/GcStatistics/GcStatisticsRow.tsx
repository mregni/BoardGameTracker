import {Col, Row} from 'antd';
import React from 'react';

import {GcStatisticsCard, StatisticsCard} from './';

type Props = {
  cards: StatisticsCard[]
}


export const GcStatisticsRow = (props: Props) => {
  const {cards} = props;
  
  return (
    <Row gutter={[16, 16]}>
      {
        cards.map(card => (
          card.value !== null &&
          <Col xs={24} md={12} xl={8} xxl={6} key={card.title}>
            <GcStatisticsCard card={card} />
          </Col>
        ))
      }
    </Row>
  )
}
