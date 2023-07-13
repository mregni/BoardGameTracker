import React from 'react';
import {useParams} from 'react-router-dom';

type Props = {}

const AddPlay = (props: Props) => {
  const { id } = useParams();
  return (
    <div>AddPlay {id}</div>
  )
}

export default AddPlay