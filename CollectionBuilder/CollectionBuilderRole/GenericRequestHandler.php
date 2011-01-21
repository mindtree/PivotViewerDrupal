<?php
header( "HTTP/1.1 301 Moved Permanently" );
header( "Status: 301 Moved Permanently" );
$qs = $_SERVER['QUERY_STRING'];
header( "Location: GenericRequestHandler.aspx?$qs" );
exit(0);
?>