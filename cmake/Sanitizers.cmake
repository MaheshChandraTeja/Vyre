include(CheckCXXCompilerFlag)

function(vyre_enable_sanitizers)
  if(MSVC)
    message(STATUS "Sanitizers: skipping (MSVC). Enable clang-cl ASan separately if you really want.")
    return()
  endif()

  set(SAN_FLAGS "-fsanitize=address,undefined -fno-omit-frame-pointer")
  check_cxx_compiler_flag("${SAN_FLAGS}" HAS_SAN)

  if(NOT HAS_SAN)
    message(WARNING "Sanitizers requested but compiler doesn't support: ${SAN_FLAGS}")
    return()
  endif()

  add_compile_options(${SAN_FLAGS})
  add_link_options(${SAN_FLAGS})
  message(STATUS "Sanitizers enabled: ${SAN_FLAGS}")
endfunction()
