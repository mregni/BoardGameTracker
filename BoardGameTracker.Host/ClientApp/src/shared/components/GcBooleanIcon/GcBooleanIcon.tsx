import React from 'react';

import {CheckCircleTwoTone, CloseCircleTwoTone} from '@ant-design/icons';

type Props = {
  value: boolean;
}

const GcBooleanIcon = (props: Props) => {
  const { value } = props;

  return (
    <>
      {value ? <CheckCircleTwoTone twoToneColor="#237804" /> : <CloseCircleTwoTone twoToneColor="#a8071a" /> }
    </>
  )
}

export default GcBooleanIcon