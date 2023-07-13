import './styles.css';

import {Badge, Button, Space} from 'antd';
import React, {Children, ReactNode, useEffect, useState} from 'react';
import {Link} from 'react-router-dom';

import {EyeOutlined, PlusOutlined} from '@ant-design/icons';

import {GameState} from '../../models';
import {GcStateRibbon} from '../GcStateRibbon';

interface OverLayProps {
  id: number;
  showPlayLink: boolean;
  detailPage: string;
}

const OverLay = (props: OverLayProps) => {
  const { id, showPlayLink, detailPage } = props;
  return (
    <div className='overlay'>
      <Space direction='vertical' size={[8, 12]} align="center">
        {showPlayLink &&
          <Link to={`/plays/${id}`}>
            <Button type="primary" icon={<PlusOutlined />} size='large' />
          </Link>}
        <Link to={`/${detailPage}/${id}`}>
          <Button type="primary" icon={<EyeOutlined />} size='large' />
        </Link>
      </Space>
    </div>
  )
}

interface Props {
  title: string;
  id: number;
  image: string;
  showPlayLink?: boolean;
  detailPage: string;
  state?: GameState;
}

export const GcCard = (props: Props) => {
  const { title, id, image, state, detailPage, showPlayLink = false } = props;
  return (
    <>
      <GcStateRibbon state={state}>
        <div className='card-image'>
          <img
            src={`https://localhost:7178/${image}`}
            alt={`poster for ${title}`}
            className='image'
          />
          <OverLay id={id} showPlayLink={showPlayLink} detailPage={detailPage} />
        </div>
      </GcStateRibbon>
      <div className='title'>{title}</div>
    </>
  )
}