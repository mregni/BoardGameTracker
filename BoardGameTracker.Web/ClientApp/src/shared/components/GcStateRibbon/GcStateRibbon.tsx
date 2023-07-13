import {Badge} from 'antd';
import React, {ReactNode, useEffect, useState} from 'react';

import {GameState} from '../../models';

interface RibbonProps {
  children: ReactNode;
  state?: GameState;
}

export const GcStateRibbon = (props: RibbonProps) => {
  const { children, state = null } = props;
  const [text, setText] = useState("");
  const [color, setColor] = useState("green");

  useEffect(() => {
    if (state === GameState.Wanted) {
      setText('Wanted');
      setColor('orange');
    } 
    else if (state === GameState.ForTrade) {
      setText('For trade');
      setColor('purple');
    }
    else if (state === GameState.PreviouslyOwned) {
      setText('Previously owned');
      setColor('blue');
    }
    else if (state === GameState.NotOwned) {
      setText('Not owned');
      setColor('red');
    }
  }, [state]);


  if (state === null || state === GameState.Owned) {
    return (<>{children}</>)
  }

  return (
    <Badge.Ribbon text={text} color={color}>
      {children}
    </Badge.Ribbon>
  )
}
