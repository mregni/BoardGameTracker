import {Card, Statistic} from 'antd';
import React from 'react';

export interface StatisticsCard {
  title: string;
  value: string | number | null;
  precision?: number | undefined;
  suffix?: string | undefined;
}

type Props = {
  card: StatisticsCard;
}

export const GcStatisticsCard = (props: Props) => {
  const {card} = props;
  
  return (
    <Card bordered={false}>
      <Statistic
        title={card.title}
        value={card.value}
        suffix={card.suffix}
        precision={card.precision}
      />
    </Card>
  )
}
